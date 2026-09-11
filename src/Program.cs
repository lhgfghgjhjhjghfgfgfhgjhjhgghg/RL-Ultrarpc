using System.Buffers.Binary;
using System.Diagnostics;
using System.IO.Pipes;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

record Config(string discordClientId,string statsApiWebSocket,string largeImageKey);

class Program {
 static async Task Main() {
  var cfg=JsonSerializer.Deserialize<Config>(File.ReadAllText(Path.Combine(AppContext.BaseDirectory,"config.json")))!;
  if(cfg.discordClientId.Contains("PUT_YOUR")) { MessageBox.Show("Put your Discord Application ID in config.json."); return; }
  using var tray=new NotifyIcon{Visible=true,Text="RL-UltraRPC",Icon=SystemIcons.Application};
  var menu=new ContextMenuStrip(); menu.Items.Add("Exit",null,(_,__)=>Environment.Exit(0)); tray.ContextMenuStrip=menu;
  while(true) try {
   using var d=new Discord(cfg.discordClientId); await d.Connect();
   using var ws=new ClientWebSocket(); await ws.ConnectAsync(new Uri(cfg.statsApiWebSocket),CancellationToken.None);
   tray.Text="RL-UltraRPC - Connected";
   string last="";
   var b=new byte[65536];
   while(ws.State==WebSocketState.Open) {
    var sb=new StringBuilder(); WebSocketReceiveResult r;
    do { r=await ws.ReceiveAsync(b,CancellationToken.None); if(r.MessageType==WebSocketMessageType.Close) break; sb.Append(Encoding.UTF8.GetString(b,0,r.Count)); } while(!r.EndOfMessage);
    var a=Activity(sb.ToString(),cfg); if(a!=null && a!=last){await d.Send(1,a);last=a;}
   }
  } catch { tray.Text="RL-UltraRPC - Waiting for Rocket League"; await Task.Delay(2000); }
 }
 static string? Activity(string raw,Config c) {
  try {
   var root=JsonNode.Parse(raw); if(root?["Event"]?.ToString()!="UpdateState") return null;
   var data=root["Data"]; if(data is JsonValue && data.ToString().StartsWith("{")) data=JsonNode.Parse(data.ToString());
   var g=data?["Game"]; if(g==null)return null;
   int blue=0,orange=0;
   if(g["Teams"] is JsonArray teams) foreach(var t in teams){var n=t?["TeamNum"]?.GetValue<int>()??-1;var s=t?["Score"]?.GetValue<int>()??0;if(n==0)blue=s;if(n==1)orange=s;}
   int sec=g["TimeSeconds"]?.GetValue<int>()??-1; bool ot=g["bOvertime"]?.GetValue<bool>()??false; int p=g["PlaylistId"]?.GetValue<int>()??-1;
   string mode=p switch{1=>"Competitive 1v1",2=>"Competitive 2v2",3=>"Competitive 3v3",10=>"Casual 1v1",11=>"Casual 2v2",13=>"Casual 3v3",_=>"Rocket League"};
   string clock=ot?"OVERTIME":sec>=0?$"{sec/60}:{sec%60:00}":"LIVE";
   return new JsonObject{
    ["cmd"]="SET_ACTIVITY",["nonce"]=Guid.NewGuid().ToString(),
    ["args"]=new JsonObject{["pid"]=Process.GetCurrentProcess().Id,
     ["activity"]=new JsonObject{["type"]=0,["details"]=mode,["state"]=$"{blue} - {orange} • {clock}",
      ["assets"]=new JsonObject{["large_image"]=c.largeImageKey}}}}.ToJsonString();
  } catch{return null;}
 }
}

sealed class Discord:IDisposable {
 readonly string id; NamedPipeClientStream? s;
 public Discord(string id)=>this.id=id;
 public async Task Connect(){
  for(int i=0;i<10&&s==null;i++){var p=new NamedPipeClientStream(".",$"discord-ipc-{i}",PipeDirection.InOut,PipeOptions.Asynchronous);try{await p.ConnectAsync(300);s=p;}catch{p.Dispose();}}
  if(s==null)throw new Exception("Discord not found");
  await Send(0,JsonSerializer.Serialize(new{v=1,client_id=id}));
  var h=new byte[8];await Read(h);int n=BinaryPrimitives.ReadInt32LittleEndian(h.AsSpan(4));if(n>0){var x=new byte[n];await Read(x);}
 }
 public async Task Send(int op,string text){var x=Encoding.UTF8.GetBytes(text);var h=new byte[8];BinaryPrimitives.WriteInt32LittleEndian(h.AsSpan(0,4),op);BinaryPrimitives.WriteInt32LittleEndian(h.AsSpan(4,4),x.Length);await s!.WriteAsync(h);await s.WriteAsync(x);await s.FlushAsync();}
 async Task Read(byte[] b){int o=0;while(o<b.Length){int n=await s!.ReadAsync(b.AsMemory(o));if(n<=0)throw new EndOfStreamException();o+=n;}}
 public void Dispose()=>s?.Dispose();
}