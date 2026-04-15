using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace WebSocketClient
{
    public class MyWebSocketClient
    {
        private ClientWebSocket _client;

        public async Task ConnectToServerAsync(string url)
        {
            _client = new ClientWebSocket();
            try
            {
                Uri uri = new Uri(url);
                Console.WriteLine($"[Client] 正在連線至 {url}...");
                // 設定連線逾時（5 秒）
                using (var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5)))
                {
                    await _client.ConnectAsync(uri, cts.Token);
                }
                Console.WriteLine("[Client] 連線成功！");
            }
            catch (WebSocketException ex)
            {
                Console.WriteLine($"[Error] WebSocket 連線失敗: {ex.Message}");
                _client?.Dispose();
            }
            catch (OperationCanceledException)
            {
                Console.WriteLine("[Error] 連線逾時 (Timeout)");
                _client?.Dispose();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Error] 發生未知錯誤: {ex.Message}");
                _client?.Dispose();
            }
        }

        public async Task SendMessageAsync(string message)
        {
            if (_client == null || _client.State != WebSocketState.Open)
            {
                Console.WriteLine("[Client] 傳送失敗：WebSocket 未連線或已關閉。");
                return;
            }
            try
            {
                byte[] sendData = Encoding.UTF8.GetBytes(message);
                var segment = new ArraySegment<byte>(sendData);
                await _client.SendAsync(
                    segment,
                    WebSocketMessageType.Text,
                    true,
                    CancellationToken.None
                );
                Console.WriteLine($"[Client] 已傳送: {message}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Client] 傳送發生異常: {ex.Message}");
            }
        }

        public async Task<string> ReceiveMessageAsync(CancellationToken ct = default)
        {
            if (_client.State != WebSocketState.Open)
                return null;
            // 使用 ArrayPool 或固定 Buffer
            var buffer = new byte[1024 * 4];
            var ms = new MemoryStream();
            try
            {
                WebSocketReceiveResult result;
                do
                {
                    // 接收資料
                    result = await _client.ReceiveAsync(new ArraySegment<byte>(buffer), ct);
                    // 如果伺服器發起關閉連線
                    if (result.MessageType == WebSocketMessageType.Close)
                    {
                        await _client.CloseAsync(WebSocketCloseStatus.NormalClosure, "Acknowledge Close", ct);
                        return null;
                    }
                    // 將接收到的位元組寫入記憶體流
                    ms.Write(buffer, 0, result.Count);

                } while (!result.EndOfMessage); // 確保完整接收（如果是分段訊息）
                ms.Seek(0, SeekOrigin.Begin);
                using (var reader = new StreamReader(ms, Encoding.UTF8))
                {
                    string message = await reader.ReadToEndAsync();
                    //Console.WriteLine($"[Server 回應]: {message}");
                    return message;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Client] 接收出錯: {ex.Message}");
                return null;
            }
        }
    }


}
