using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.IO;

namespace WebSocketServer
{
    public class MyWebSocketServer
    {
        private HttpListener _listener;

        public async Task StartAsync(string prefix)
        {
            _listener = new HttpListener();
            _listener.Prefixes.Add(prefix);
            _listener.Start();
            Console.WriteLine($"[Server] 已啟動，監聽: {prefix}");

            while (_listener.IsListening)
            {
                try
                {
                    var context = await _listener.GetContextAsync();
                    if (context.Request.IsWebSocketRequest)
                    {
                        // 針對每個新連線開啟獨立 Task 處理
                        _ = Task.Run(() => HandleClientAsync(context));
                    }
                    else
                    {
                        context.Response.StatusCode = 400;
                        context.Response.Close();
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("[Server] 接收請求錯誤: " + ex.Message);
                }
            }
        }

        #region For String
        // 1.接收
        public async Task<string> ReceiveAsync(WebSocket ws)
        {
            var buffer = new byte[4096];
            using (var ms = new System.IO.MemoryStream())
            {
                WebSocketReceiveResult result;
                do
                {
                    result = await ws.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
                    if (result.MessageType == WebSocketMessageType.Close)
                    {
                        await ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", CancellationToken.None);
                        return null;
                    }
                    ms.Write(buffer, 0, result.Count);
                } while (!result.EndOfMessage);
                return Encoding.UTF8.GetString(ms.ToArray());
            }
        }

        // 2.回傳
        public async Task SendAsync(WebSocket ws, string message)
        {
            if (ws.State != WebSocketState.Open) return;
            byte[] data = Encoding.UTF8.GetBytes(message);
            await ws.SendAsync(
                new ArraySegment<byte>(data),
                WebSocketMessageType.Text,
                true,
                CancellationToken.None
            );
        }
        #endregion

        #region for file
        public async Task ReceiveFile(WebSocket ws, string filepath)
        {
            // 確保目錄存在
            string dir = Path.GetDirectoryName(filepath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir)) Directory.CreateDirectory(dir);

            using (var fs = new FileStream(filepath, FileMode.Create, FileAccess.Write))
            {
                var buffer = new byte[1024 * 64];
                WebSocketReceiveResult result;

                do
                {
                    result = await ws.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);

                    if (result.MessageType == WebSocketMessageType.Close) return;

                    if (result.MessageType == WebSocketMessageType.Binary)
                    {
                        await fs.WriteAsync(buffer, 0, result.Count);
                    }

                    // 只要 EndOfMessage 為 false，就表示這個檔案還沒傳完
                } while (!result.EndOfMessage);
            }

            Console.WriteLine($"檔案 {filepath} 接收完成。");
        }
        #endregion

        private async Task HandleClientAsync(HttpListenerContext context)
        {
            HttpListenerWebSocketContext wsContext = await context.AcceptWebSocketAsync(null);
            WebSocket ws = wsContext.WebSocket;
            Console.WriteLine("[Server] Client 已連線！");
            try
            {
                while (ws.State == WebSocketState.Open)
                {
                    await Task.Delay(2000);
                    await SendAsync(ws, "Tray IC放置完成，請雷射掃描!");
                    Console.WriteLine("告知Client雷射掃描!");
                    string receivedMessage = await ReceiveAsync(ws);
                    Console.WriteLine($"[收到檢測訊息]: {receivedMessage}");
                    await ReceiveFile(ws, @"D:\Chimingkuei\repos\WebSocket\Output\Test.json");
                    Console.WriteLine($"[收到檢測檔案]: {receivedMessage}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("[Server] 連線處理異常: " + ex.Message);
            }
            finally
            {
                ws.Dispose();
                Console.WriteLine("[Server] 連線已關閉。");
            }
        }
    }

}
