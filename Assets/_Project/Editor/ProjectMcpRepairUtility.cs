using System.Linq;
using System.Threading.Tasks;
using MCPForUnity.Editor.Clients;
using MCPForUnity.Editor.Services;
using MCPForUnity.Editor.Services.Transport;
using UnityEditor;
using UnityEngine;

namespace Project.EditorTools
{
    [InitializeOnLoad]
    public static class ProjectMcpRepairUtility
    {
        private const string SessionAutoConnectKey = "ProjectMcpRepairUtility.AutoConnectAttempted";

        static ProjectMcpRepairUtility()
        {
            EditorApplication.delayCall += QueueAutoConnectAfterEditorStable;
        }

        private static void QueueAutoConnectAfterEditorStable()
        {
            EditorApplication.update -= WaitForStableEditorBeforeAutoConnect;
            EditorApplication.update += WaitForStableEditorBeforeAutoConnect;
        }

        private static void WaitForStableEditorBeforeAutoConnect()
        {
            if (EditorApplication.isCompiling || EditorApplication.isUpdating)
                return;

            EditorApplication.update -= WaitForStableEditorBeforeAutoConnect;
            TryAutoConnectBridgeOnce();
        }

        [MenuItem(SurvivalPioneerEditorMenus.Maintenance + "Repair Cursor MCP Connection", false, 0)]
        public static void RepairCursorMcpConnectionMenu()
        {
            RepairCursorMcpConnection(showDialog: true);
        }

        public static void RepairCursorMcpConnection(bool showDialog)
        {
            IMcpClientConfigurator cursorConfigurator = FindCursorConfigurator();
            if (cursorConfigurator == null)
            {
                if (showDialog)
                {
                    EditorUtility.DisplayDialog(
                        "Repair Cursor MCP",
                        "Cursor MCP configurator was not found. Open Window > MCP For Unity and run Auto Configure for Cursor.",
                        "OK");
                }

                return;
            }

            try
            {
                MCPServiceLocator.Client.ConfigureClient(cursorConfigurator);
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"ProjectMcpRepairUtility: Failed to configure Cursor MCP: {ex.Message}");
                if (showDialog)
                {
                    EditorUtility.DisplayDialog(
                        "Repair Cursor MCP",
                        $"Could not update Cursor config:\n\n{ex.Message}",
                        "OK");
                }

                return;
            }

            bool serverStarted = MCPServiceLocator.Server.IsLocalHttpServerReachable()
                || MCPServiceLocator.Server.StartLocalHttpServer(quiet: true);

            if (!serverStarted)
            {
                if (showDialog)
                {
                    EditorUtility.DisplayDialog(
                        "Repair Cursor MCP",
                        "Updated Cursor config, but the local MCP HTTP server could not be started. Check Window > MCP For Unity.",
                        "OK");
                }

                return;
            }

            _ = ConnectBridgeAsync(showDialog);
        }

        private static async Task ConnectBridgeAsync(bool showDialog)
        {
            bool started = await MCPServiceLocator.Bridge.StartAsync();
            if (showDialog)
            {
                EditorUtility.DisplayDialog(
                    "Repair Cursor MCP",
                    started
                        ? "Cursor MCP config, HTTP server, and Unity bridge are connected. Restart Cursor if tools still do not appear."
                        : "Cursor config and HTTP server were updated, but the Unity bridge failed to connect. Open Window > MCP For Unity and click Start Session.",
                    "OK");
            }

            if (started)
                Debug.Log("ProjectMcpRepairUtility: Unity MCP bridge connected.");
            else
                Debug.LogWarning("ProjectMcpRepairUtility: Unity MCP bridge failed to connect.");
        }

        private static void TryAutoConnectBridgeOnce()
        {
            if (SessionState.GetBool(SessionAutoConnectKey, false))
                return;

            SessionState.SetBool(SessionAutoConnectKey, true);

            if (!EditorConfigurationCache.Instance.UseHttpTransport)
                return;

            if (MCPServiceLocator.TransportManager.IsRunning(TransportMode.Http))
                return;

            if (!MCPServiceLocator.Server.IsLocalHttpServerReachable())
                return;

            _ = ConnectBridgeAsync(showDialog: false);
        }

        private static IMcpClientConfigurator FindCursorConfigurator()
        {
            return McpClientRegistry.All.FirstOrDefault(
                configurator => configurator.DisplayName == "Cursor");
        }
    }
}
