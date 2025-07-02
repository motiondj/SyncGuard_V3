// Copyright Epic Games, Inc. All Rights Reserved.

#include "LiveLinkHubProvider.h"

#include "Algo/Transform.h"
#include "Async/Async.h"
#include "Clients/LiveLinkHubClientsModel.h"
#include "Clients/LiveLinkHubUEClientInfo.h"
#include "Containers/ObservableArray.h"
#include "CoreMinimal.h"
#include "Delegates/DelegateCombinations.h"
#include "Editor.h"
#include "HAL/CriticalSection.h"
#include "INetworkMessagingExtension.h"
#include "LiveLinkHubLog.h"
#include "LiveLinkHubMessages.h"
#include "LiveLinkSettings.h"
#include "MessageEndpointBuilder.h"
#include "Misc/ScopeLock.h"
#include "Session/LiveLinkHubSession.h"
#include "Session/LiveLinkHubSessionManager.h"
#include "Settings/LiveLinkHubSettings.h"
#include "TimerManager.h"



#define LOCTEXT_NAMESPACE "LiveLinkHub.LiveLinkHubProvider"

namespace LiveLinkHubProviderUtils
{
	static INetworkMessagingExtension* GetMessagingStatistics()
	{
		IModularFeatures& ModularFeatures = IModularFeatures::Get();

		if (IsInGameThread())
		{
			if (ModularFeatures.IsModularFeatureAvailable(INetworkMessagingExtension::ModularFeatureName))
			{
				return &ModularFeatures.GetModularFeature<INetworkMessagingExtension>(INetworkMessagingExtension::ModularFeatureName);
			}
		}
		else
		{
			IModularFeatures::FScopedLockModularFeatureList ScopedLockModularFeatureList;

			if (ModularFeatures.IsModularFeatureAvailable(INetworkMessagingExtension::ModularFeatureName))
			{
				return &ModularFeatures.GetModularFeature<INetworkMessagingExtension>(INetworkMessagingExtension::ModularFeatureName);
			}
		}


		ensureMsgf(false, TEXT("Feature %s is unavailable"), *INetworkMessagingExtension::ModularFeatureName.ToString());
		return nullptr;
	}

	FString GetIPAddress(const FMessageAddress& ClientAddress)
	{
		FString IPAddress;
		if (INetworkMessagingExtension* Statistics = GetMessagingStatistics())
		{
			const FGuid NodeId = Statistics->GetNodeIdFromAddress(ClientAddress);
			IPAddress = NodeId.IsValid() ? Statistics->GetLatestNetworkStatistics(NodeId).IPv4AsString : FString();

			int32 PortIndex = INDEX_NONE;
			IPAddress.FindChar(TEXT(':'), PortIndex);

			// Cut off the port from the end.
			if (PortIndex != INDEX_NONE)
			{
				IPAddress.LeftInline(PortIndex);
			}
		}
		return IPAddress;
	}
}


FLiveLinkHubProvider::FLiveLinkHubProvider(const TSharedRef<ILiveLinkHubSessionManager>& InSessionManager)
	: FLiveLinkProvider(TEXT("LiveLink Hub"), false)
	, SessionManager(InSessionManager)
{
	Annotations.Add(FLiveLinkHubMessageAnnotation::ProviderTypeAnnotation, UE::LiveLinkHub::Private::LiveLinkHubProviderType.ToString());

	FMessageEndpointBuilder EndpointBuilder = FMessageEndpoint::Builder(*GetProviderName());
	EndpointBuilder.WithHandler(MakeHandler<FLiveLinkClientInfoMessage>(&FLiveLinkHubProvider::HandleClientInfoMessage));
	EndpointBuilder.WithHandler(MakeHandler<FLiveLinkHubConnectMessage>(&FLiveLinkHubProvider::HandleHubConnectMessage));

	CreateMessageEndpoint(EndpointBuilder);

	const double ValidateConnectionsRate = GetDefault<ULiveLinkSettings>()->MessageBusPingRequestFrequency;
	GEditor->GetTimerManager()->SetTimer(ValidateConnectionsTimer, FTimerDelegate::CreateRaw(this, &FLiveLinkHubProvider::ValidateConnections), ValidateConnectionsRate, true);
}

FLiveLinkHubProvider::~FLiveLinkHubProvider()
{
	if (GEditor)
	{
		GEditor->GetTimerManager()->ClearTimer(ValidateConnectionsTimer);
	}
}

bool FLiveLinkHubProvider::ShouldTransmitToSubject_AnyThread(FName SubjectName, FMessageAddress Address) const
{
	auto AdditionalFilter = [SubjectName](const FLiveLinkHubUEClientInfo* ClientInfoPtr)
	{
		return !ClientInfoPtr->DisabledSubjects.Contains(SubjectName);
	};

	return ShouldTransmitToClient_AnyThread(Address, AdditionalFilter);
}

void FLiveLinkHubProvider::UpdateTimecodeSettings(const FLiveLinkHubTimecodeSettings& InSettings, const FLiveLinkHubClientId& ClientId)
{
	SendTimecodeSettings(InSettings, ClientId);
}

void FLiveLinkHubProvider::ResetTimecodeSettings(const FLiveLinkHubClientId& ClientId)
{
	// Sending settings with ELiveLinkHubTimecodeSource::NotDefined will reset the timecode on the client.
	SendTimecodeSettings(FLiveLinkHubTimecodeSettings{}, ClientId);
}

void FLiveLinkHubProvider::SendTimecodeSettings(const FLiveLinkHubTimecodeSettings& InSettings, const FLiveLinkHubClientId& ClientId)
{
	if (ClientId.IsValid())
	{
		TArray<FMessageAddress> AllAddresses;
		GetConnectedAddresses(AllAddresses);

		FMessageAddress* TargetAddress = AllAddresses.FindByPredicate([this, ClientId](const FMessageAddress& Address)
		{
			return AddressToIdCache.FindChecked(Address) == ClientId;
		});

		if (TargetAddress)
		{
			SendMessage(FMessageEndpoint::MakeMessage<FLiveLinkHubTimecodeSettings>(InSettings));
		}
	}
	else
	{
		// Invalid ID means we're broadcasting to all clients.
		SendMessageToEnabledClients(FMessageEndpoint::MakeMessage<FLiveLinkHubTimecodeSettings>(InSettings));
	}

}

void FLiveLinkHubProvider::AddRestoredClient(FLiveLinkHubUEClientInfo& RestoredClientInfo)
{
	// If a client was already discovered with the same hostname, update it to match the restored client.
	bool bMatchedExistingConnection = false;
	if (const TSharedPtr<ILiveLinkHubSessionManager> Manager = SessionManager.Pin())
	{
		if (const TSharedPtr<ILiveLinkHubSession> ActiveSession = Manager->GetCurrentSession())
		{
			FReadScopeLock Locker(ClientsMapLock);

			for (auto It = ClientsMap.CreateIterator(); It; ++It)
			{
				FLiveLinkHubUEClientInfo& IteratedClient = It->Value;
				if (IteratedClient.Hostname == RestoredClientInfo.Hostname && !ActiveSession->IsClientInSession(It->Key))
				{
					bMatchedExistingConnection = true;

					// Update Client info from the new connection.
					RestoredClientInfo = It->Value;
					break;
				}
			}
		}
	}

	if (!bMatchedExistingConnection)
	{
		ClientsMap.Add(RestoredClientInfo.Id, RestoredClientInfo);
	}

	OnClientEventDelegate.Broadcast(RestoredClientInfo.Id, EClientEventType::Discovered);
}

TOptional<FLiveLinkHubUEClientInfo> FLiveLinkHubProvider::GetClientInfo(FLiveLinkHubClientId InClient) const
{
	FReadScopeLock Locker(ClientsMapLock);
	TOptional<FLiveLinkHubUEClientInfo> ClientInfo;
	if (const FLiveLinkHubUEClientInfo* ClientInfoPtr = ClientsMap.Find(InClient))
	{
		ClientInfo = *ClientInfoPtr;
	}

	return ClientInfo;
}

void FLiveLinkHubProvider::HandleHubConnectMessage(const FLiveLinkHubConnectMessage& Message, const TSharedRef<IMessageContext, ESPMode::ThreadSafe>& Context)
{
	FLiveLinkConnectMessage ConnectMessage;
	ConnectMessage.LiveLinkVersion = Message.ClientInfo.LiveLinkVersion;
	FLiveLinkProvider::HandleConnectMessage(ConnectMessage, Context);

	const FMessageAddress ConnectionAddress = Context->GetSender();

	TOptional<FLiveLinkHubClientId> UpdatedClient;
	{
		FWriteScopeLock Locker(ClientsMapLock);

		// First check if there are multiple disconnected entries with the same host as the incoming client.
		uint8 NumClientsForHost = 0;
		for (auto It = ClientsMap.CreateIterator(); It; ++It)
		{
			FLiveLinkHubUEClientInfo& IteratedClient = It->Value;
			if (IteratedClient.Hostname == Message.ClientInfo.Hostname && IteratedClient.Status == ELiveLinkClientStatus::Disconnected)
			{
				NumClientsForHost++;
				if (NumClientsForHost > 1)
				{
					break;
				}
			}
		}

		// Remove old entries if one is found
		for (auto It = ClientsMap.CreateIterator(); It; ++It)
		{
			FLiveLinkHubUEClientInfo& IteratedClient = It->Value;
			// If there are multiple disconnected clients with the same hostname, try finding a client with the same project.
			bool bFindWithMatchingProject = NumClientsForHost > 1;

			// Only replace disconnected clients to support multiple UE instances on the same host.
			if (IteratedClient.Status == ELiveLinkClientStatus::Disconnected && IteratedClient.Hostname == Message.ClientInfo.Hostname)
			{
				if (!bFindWithMatchingProject || IteratedClient.ProjectName == Message.ClientInfo.ProjectName)
				{
					IteratedClient.UpdateFromInfoMessage(Message.ClientInfo);
					IteratedClient.Id = It->Key;
					IteratedClient.Status = ELiveLinkClientStatus::Connected;

					AddressToIdCache.FindOrAdd(ConnectionAddress) = IteratedClient.Id;

					UpdatedClient = IteratedClient.Id;
					break;
				}
            }
        }
	}

	if (UpdatedClient)
	{
		// Just updated the map
		OnClientEventDelegate.Broadcast(*UpdatedClient, EClientEventType::Reestablished);
	}
	else
	{
		// Actually added a new entry in the map.
		FLiveLinkHubUEClientInfo NewClient{Message.ClientInfo};
		NewClient.IPAddress = LiveLinkHubProviderUtils::GetIPAddress(ConnectionAddress);

		const FLiveLinkHubClientId NewClientId = NewClient.Id;
		UpdatedClient = NewClientId; // Set this so we can update the client's timecode provider down below.

		{
			FWriteScopeLock Locker(ClientsMapLock);
			AddressToIdCache.FindOrAdd(ConnectionAddress) = NewClientId;
			ClientsMap.Add(NewClient.Id, MoveTemp(NewClient));
		}

		if (GetDefault<ULiveLinkHubSettings>()->bAutoAddDiscoveredClients)
		{
			AsyncTask(ENamedThreads::GameThread, [WeakSessionManager = SessionManager, NewClientId]()
			{
				if (const TSharedPtr<ILiveLinkHubSessionManager> Manager = WeakSessionManager.Pin())
				{
					if (const TSharedPtr<ILiveLinkHubSession> CurrentSession = Manager->GetCurrentSession())
					{
						CurrentSession->AddClient(NewClientId);
					}
				}
			});
		}
		else
		{
			OnClientEventDelegate.Broadcast(NewClientId, EClientEventType::Discovered);
		}
	}

	// Update the timecode provider when a client establishes connection.
	if (const TSharedPtr<ILiveLinkHubSessionManager> Manager = SessionManager.Pin())
	{
		if (const TSharedPtr<ILiveLinkHubSession> ActiveSession = Manager->GetCurrentSession())
		{
			if (ActiveSession->ShouldUseLiveLinkHubAsTimecodeSource())
			{
				SendTimecodeSettings(ActiveSession->GetTimecodeSettings(), *UpdatedClient);
			}
		}
	}
}
	
void FLiveLinkHubProvider::HandleClientInfoMessage(const FLiveLinkClientInfoMessage& Message, const TSharedRef<IMessageContext, ESPMode::ThreadSafe>& Context)
{
	FMessageAddress Address = Context->GetSender();

	FLiveLinkHubClientId ClientId = AddressToIdCache.FindRef(Address);
	{
		FWriteScopeLock Locker(ClientsMapLock);
		if (FLiveLinkHubUEClientInfo* ClientInfo = ClientsMap.Find(ClientId))
		{
			ClientInfo->UpdateFromInfoMessage(Message);
		}
	}

	if (const TSharedPtr<ILiveLinkHubSessionManager> Manager = SessionManager.Pin())
	{
		if (const TSharedPtr<ILiveLinkHubSession> ActiveSession = Manager->GetCurrentSession())
		{
			if (ActiveSession->ShouldUseLiveLinkHubAsTimecodeSource() && ClientId.IsValid())
			{
				SendTimecodeSettings(ActiveSession->GetTimecodeSettings(), ClientId);
			}
		}
	}

	if (ClientId.IsValid())
	{
		OnClientEventDelegate.Broadcast(ClientId, EClientEventType::Modified);
	}
}

bool FLiveLinkHubProvider::ShouldTransmitToClient_AnyThread(FMessageAddress Address, TFunctionRef<bool(const FLiveLinkHubUEClientInfo* ClientInfoPtr)> AdditionalFilter) const
{
	if (!Address.IsValid())
	{
		return false;
	}

	FReadScopeLock Locker(ClientsMapLock);

	const FLiveLinkHubClientId ClientId = AddressToIdCache.FindRef(Address);
	if (const FLiveLinkHubUEClientInfo* ClientInfoPtr = ClientsMap.Find(ClientId))
	{
		if (const TSharedPtr<ILiveLinkHubSessionManager> Manager = SessionManager.Pin())
		{
			if (const TSharedPtr<ILiveLinkHubSession> CurrentSession = Manager->GetCurrentSession())
			{
				if (!CurrentSession->IsClientInSession(ClientInfoPtr->Id))
				{
					return false;
				}
			}
		}

		if (!ClientInfoPtr->bEnabled)
		{
			return false;
		}

		return AdditionalFilter(ClientInfoPtr);
	}
	else
	{
		UE_LOG(LogLiveLinkHub, Warning, TEXT("Attempted to transmit data to an invalid client."));
	}

	return true;
}

void FLiveLinkHubProvider::OnConnectionsClosed(const TArray<FMessageAddress>& ClosedAddresses)
{
	// List of OldId -> NewId
	TArray<FLiveLinkHubClientId> Notifications;
	TArray<FMessageAddress> AddressesToRemove;
	{
		FWriteScopeLock Locker(ClientsMapLock);

		for (FMessageAddress TrackedAddress : ClosedAddresses)
		{
			FLiveLinkHubClientId ClientId = AddressToIdCache.FindRef(TrackedAddress);
			if (FLiveLinkHubUEClientInfo* FoundInfo = ClientsMap.Find(ClientId))
			{
				FoundInfo->Status = ELiveLinkClientStatus::Disconnected;
				Notifications.Add(ClientId);
			}
		}
	}

	for (const FLiveLinkHubClientId& Client : Notifications)
	{
		OnClientEventDelegate.Broadcast(Client, EClientEventType::Disconnected);
	}

	for (FMessageAddress TrackedAddress : ClosedAddresses)
	{
		FWriteScopeLock Locker(ClientsMapLock);
		AddressToIdCache.Remove(TrackedAddress);
	}
}

TArray<FLiveLinkHubClientId> FLiveLinkHubProvider::GetSessionClients() const
{
	TArray<FLiveLinkHubClientId> SessionClients;

	if (const TSharedPtr<ILiveLinkHubSessionManager> Manager = SessionManager.Pin())
	{
		if (const TSharedPtr<ILiveLinkHubSession> CurrentSession = Manager->GetCurrentSession())
		{
			SessionClients = CurrentSession->GetSessionClients();
		}
	}

	return SessionClients;
}

TMap<FName, FString> FLiveLinkHubProvider::GetAnnotations() const
{
	return Annotations;
}

TArray<FLiveLinkHubClientId> FLiveLinkHubProvider::GetDiscoveredClients() const
{
	TArray<FLiveLinkHubClientId> DiscoveredClients;

	if (const TSharedPtr<ILiveLinkHubSessionManager> Manager = SessionManager.Pin())
	{
		if (const TSharedPtr<ILiveLinkHubSession> CurrentSession = Manager->GetCurrentSession())
		{
			FReadScopeLock Locker(ClientsMapLock);
			TArray<FLiveLinkHubClientId> SessionClients = CurrentSession->GetSessionClients();
			for (const TPair<FLiveLinkHubClientId, FLiveLinkHubUEClientInfo>& ClientPair : ClientsMap)
			{
				if (ClientPair.Value.Status != ELiveLinkClientStatus::Disconnected && !SessionClients.Contains(ClientPair.Key))
				{
					DiscoveredClients.Add(ClientPair.Key);
				}
			}
		}
	}

	return DiscoveredClients;
}

FText FLiveLinkHubProvider::GetClientDisplayName(FLiveLinkHubClientId InAddress) const
{
	FReadScopeLock Locker(ClientsMapLock);
	FText DisplayName;

	if (const FLiveLinkHubUEClientInfo* ClientInfoPtr = ClientsMap.Find(InAddress))
	{
		DisplayName = FText::FromString(FString::Format(TEXT("{0} ({1})"), {*ClientInfoPtr->Hostname, *ClientInfoPtr->CurrentLevel}));
	}
	else
	{
		DisplayName = LOCTEXT("InvalidClientLabel", "Invalid Client");
	}

	return DisplayName;
}

FText FLiveLinkHubProvider::GetClientStatus(FLiveLinkHubClientId Client) const
{
	FReadScopeLock Locker(ClientsMapLock);
	if (const FLiveLinkHubUEClientInfo* ClientInfoPtr = ClientsMap.Find(Client))
	{
		return StaticEnum<ELiveLinkClientStatus>()->GetDisplayNameTextByValue(static_cast<int64>(ClientInfoPtr->Status));
	}
		
	return LOCTEXT("InvalidStatus", "Disconnected");
}

bool FLiveLinkHubProvider::IsClientEnabled(FLiveLinkHubClientId Client) const
{
	FReadScopeLock Locker(ClientsMapLock);
	if (const FLiveLinkHubUEClientInfo* ClientInfoPtr = ClientsMap.Find(Client))
	{
		return ClientInfoPtr->bEnabled;
	}
	return false;
}

bool FLiveLinkHubProvider::IsClientConnected(FLiveLinkHubClientId Client) const
{
	FReadScopeLock Locker(ClientsMapLock);
	if (const FLiveLinkHubUEClientInfo* ClientInfoPtr = ClientsMap.Find(Client))
	{
		return ClientInfoPtr->Status == ELiveLinkClientStatus::Connected;
	}
	return false;
}

void FLiveLinkHubProvider::SetClientEnabled(FLiveLinkHubClientId Client, bool bInEnable)
{
	{
		FWriteScopeLock Locker(ClientsMapLock);
		if (FLiveLinkHubUEClientInfo* ClientInfoPtr = ClientsMap.Find(Client))
		{
			ClientInfoPtr->bEnabled = bInEnable;
		}
	}

	if (const TSharedPtr<ILiveLinkHubSessionManager> Manager = SessionManager.Pin())
	{
		if (const TSharedPtr<ILiveLinkHubSession> ActiveSession = Manager->GetCurrentSession())
		{
			if (ActiveSession->ShouldUseLiveLinkHubAsTimecodeSource())
			{
				if (bInEnable)
				{
					// Enabling client, send it up to date timecode settings.
					SendTimecodeSettings(ActiveSession->GetTimecodeSettings(), Client);
				}
				else
				{
					// Disabling it, so reset its timecode provider.
					ResetTimecodeSettings(Client);
				}
			}
		}
	}
}

bool FLiveLinkHubProvider::IsSubjectEnabled(FLiveLinkHubClientId Client, FName SubjectName) const
{
	FReadScopeLock Locker(ClientsMapLock);
	if (const FLiveLinkHubUEClientInfo* ClientInfoPtr = ClientsMap.Find(Client))
	{
		return !ClientInfoPtr->DisabledSubjects.Contains(SubjectName);
	}
	return false;
}

void FLiveLinkHubProvider::SetSubjectEnabled(FLiveLinkHubClientId Client, FName SubjectName, bool bInEnable)
{
	FWriteScopeLock Locker(ClientsMapLock);
	if (FLiveLinkHubUEClientInfo* ClientInfoPtr = ClientsMap.Find(Client))
	{
		if (bInEnable)
		{
			ClientInfoPtr->DisabledSubjects.Remove(SubjectName);
		}
		else
		{
			ClientInfoPtr->DisabledSubjects.Add(SubjectName);
		}
	}
}

#undef LOCTEXT_NAMESPACE /*LiveLinkHub.LiveLinkHubProvider*/
