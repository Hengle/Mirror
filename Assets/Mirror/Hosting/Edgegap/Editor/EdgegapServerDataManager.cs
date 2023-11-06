using IO.Swagger.Model;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Edgegap
{
    // MIRROR CHANGE: EdgegapServerDataManagerUtils were merged into EdgegapServerDataManager to reduce complexity & dependencies
    // static class EdgegapServerDataManagerUtils {}
    // END MIRROR CHANGE

    /// <summary>
    /// Utility class to centrally manage the Edgegap server data, and create / update the elements displaying the server info.
    /// </summary>
    public static class EdgegapServerDataManager
    {
        static Status _serverData;
        static ApiEnvironment _apiEnvironment;

        // UI elements
        static readonly StyleSheet _serverDataStylesheet;
        static readonly List<VisualElement> _serverDataContainers = new List<VisualElement>();

        public static Status GetServerStatus() => _serverData;

        static EdgegapServerDataManager()
        {
            // MIRROR CHANGE
            _serverDataStylesheet = AssetDatabase.LoadAssetAtPath<StyleSheet>($"{EdgegapWindow.StylesheetPath}/EdgegapServerData.uss");
            // END MIRROR CHANGE
        }
        public static void RegisterServerDataContainer(VisualElement serverDataContainer)
        {
            _serverDataContainers.Add(serverDataContainer);
        }
        public static void DeregisterServerDataContainer(VisualElement serverDataContainer)
        {
            _serverDataContainers.Remove(serverDataContainer);
        }
        public static void SetServerData(Status serverData, ApiEnvironment apiEnvironment)
        {
            _serverData = serverData;
            _apiEnvironment = apiEnvironment;
            RefreshServerDataContainers();
        }


        public static Label GetHeader(string text)
        {
            Label header = new Label(text);
            header.AddToClassList("label__header");

            return header;
        }

        public static VisualElement GetHeaderRow()
        {
            VisualElement row = new VisualElement();
            row.AddToClassList("row__port-table");
            row.AddToClassList("label__header");

            row.Add(new Label("Name"));
            row.Add(new Label("External"));
            row.Add(new Label("Internal"));
            row.Add(new Label("Protocol"));
            row.Add(new Label("Link"));

            return row;
        }

        public static VisualElement GetRowFromPortResponse(PortMapping port)
        {
            VisualElement row = new VisualElement();
            row.AddToClassList("row__port-table");
            row.AddToClassList("focusable");


            row.Add(new Label(port.Name));
            row.Add(new Label(port.External.ToString()));
            row.Add(new Label(port.Internal.ToString()));
            row.Add(new Label(port.Protocol));
            row.Add(GetCopyButton("Copy", port.Link));

            return row;
        }

        public static Button GetCopyButton(string btnText, string copiedText)
        {
            Button copyBtn = new Button();
            copyBtn.text = btnText;
            copyBtn.clickable.clicked += () => GUIUtility.systemCopyBuffer = copiedText;

            return copyBtn;
        }

        public static Button GetLinkButton(string btnText, string targetUrl)
        {
            Button copyBtn = new Button();
            copyBtn.text = btnText;
            copyBtn.clickable.clicked += () => UnityEngine.Application.OpenURL(targetUrl);

            return copyBtn;
        }
        public static Label GetInfoText(string innerText)
        {
            Label infoText = new Label(innerText);
            infoText.AddToClassList("label__info-text");

            return infoText;
        }

        static VisualElement GetStatusSection()
        {
            ServerStatus serverStatus = _serverData.GetServerStatus();
            string dashboardUrl = _apiEnvironment.GetDashboardUrl();
            string requestId = _serverData.RequestId;
            string deploymentDashboardUrl = "";

            if (!string.IsNullOrEmpty(requestId) && !string.IsNullOrEmpty(dashboardUrl))
            {
                deploymentDashboardUrl = $"{dashboardUrl}/arbitrium/deployment/read/{requestId}/";
            }

            VisualElement container = new VisualElement();
            container.AddToClassList("container");

            container.Add(GetHeader("Server Status"));

            VisualElement row = new VisualElement();
            row.AddToClassList("row__status");

            // Status pill
            Label statusLabel = new Label(serverStatus.GetLabelText());
            statusLabel.AddToClassList(serverStatus.GetStatusBgClass());
            statusLabel.AddToClassList("label__status");
            row.Add(statusLabel);

            // Link to dashboard
            if (!string.IsNullOrEmpty(deploymentDashboardUrl))
            {
                row.Add(GetLinkButton("See in the dashboard", deploymentDashboardUrl));
            }
            else
            {
                row.Add(new Label("Could not resolve link to this deployment"));
            }

            container.Add(row);

            return container;
        }

        static VisualElement GetDnsSection()
        {
            string serverDns = _serverData.Fqdn;

            VisualElement container = new VisualElement();
            container.AddToClassList("container");

            container.Add(GetHeader("Server DNS"));

            VisualElement row = new VisualElement();
            row.AddToClassList("row__dns");
            row.AddToClassList("focusable");

            row.Add(new Label(serverDns));
            row.Add(GetCopyButton("Copy", serverDns));

            container.Add(row);

            return container;
        }

        static VisualElement GetPortsSection()
        {
            List<PortMapping> serverPorts = _serverData.Ports.Values.ToList();

            VisualElement container = new VisualElement();
            container.AddToClassList("container");

            container.Add(GetHeader("Server Ports"));
            container.Add(GetHeaderRow());

            VisualElement portList = new VisualElement();

            if (serverPorts.Count > 0)
            {
                foreach (PortMapping port in serverPorts)
                {
                    portList.Add(GetRowFromPortResponse(port));
                }
            }
            else
            {
                portList.Add(new Label("No port configured for this app version."));
            }

            container.Add(portList);

            return container;
        }

        public static VisualElement GetServerDataVisualTree()
        {
            VisualElement serverDataTree = new VisualElement();
            serverDataTree.styleSheets.Add(_serverDataStylesheet);

            bool hasServerData = _serverData != null;
            bool isReady = hasServerData && _serverData.GetServerStatus().IsOneOf(ServerStatus.Ready, ServerStatus.Error);

            if (hasServerData)
            {
                serverDataTree.Add(GetStatusSection());

                if (isReady)
                {
                    serverDataTree.Add(GetDnsSection());
                    serverDataTree.Add(GetPortsSection());
                }
                else
                {
                    serverDataTree.Add(GetInfoText("Additionnal information will be displayed when the server is ready."));
                }
            }
            else
            {
                serverDataTree.Add(GetInfoText("Server data will be displayed here when a server is running."));
            }

            return serverDataTree;
        }

        static void RefreshServerDataContainers()
        {
            foreach (VisualElement serverDataContainer in _serverDataContainers)
            {
                serverDataContainer.Clear();
                serverDataContainer.Add(GetServerDataVisualTree()); // Cannot reuse a same instance of VisualElement
            }
        }
    }
}
