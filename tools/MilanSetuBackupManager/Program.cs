using System.Diagnostics;
using System.Net;
using System.Text;
using System.Text.Json;

namespace MilanSetuBackupManager;

internal static class Program
{
    [STAThread]
    static void Main()
    {
        ApplicationConfiguration.Initialize();
        Application.Run(new MainForm());
    }
}

internal sealed class MainForm : Form
{
    readonly TextBox config = new() { Dock = DockStyle.Fill };
    readonly Label status = new() { AutoSize = true, Text = "Ready" };
    readonly Button backup = new() { Text = "Backup Now", AutoSize = true };
    readonly Button schedule = new() { Text = "Install Daily Schedule", AutoSize = true };
    readonly Button web = new() { Text = "Open Web Dashboard", AutoSize = true };
    readonly Button folder = new() { Text = "Open Backup Folder", AutoSize = true };
    HttpListener? listener;
    CancellationTokenSource? serverCts;

    public MainForm()
    {
        Text = "MilanSetu Backup Manager";
        Width = 720; Height = 320;
        StartPosition = FormStartPosition.CenterScreen;
        config.Text = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "MilanSetuBackup", "backup-config.json");

        var title = new Label { Text = "MilanSetu Backup Manager", Font = new Font(Font.FontFamily, 18, FontStyle.Bold), AutoSize = true };
        var info = new Label { Text = "PowerShell remains the backup engine. This desktop app provides a simple Windows UI and local web dashboard.", AutoSize = true, MaximumSize = new Size(650, 45) };
        var row = new TableLayoutPanel { Dock = DockStyle.Top, Height = 48, ColumnCount = 2 };
        row.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 105)); row.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        row.Controls.Add(new Label { Text = "Config file:", Anchor = AnchorStyles.Left, AutoSize = true }, 0, 0); row.Controls.Add(config, 1, 0);
        var buttons = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 52, AutoSize = true };
        buttons.Controls.Add(backup); buttons.Controls.Add(schedule); buttons.Controls.Add(web); buttons.Controls.Add(folder);
        var panel = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown, Padding = new Padding(20), WrapContents = false };
        panel.Controls.Add(title); panel.Controls.Add(info); panel.Controls.Add(row); panel.Controls.Add(buttons); panel.Controls.Add(status);
        Controls.Add(panel);

        backup.Click += (_, _) => RunScript("backup-milansetu.ps1");
        schedule.Click += (_, _) => RunScript("setup-daily-backup.ps1");
        folder.Click += (_, _) => OpenBackupFolder();
        web.Click += async (_, _) => { await StartWebAsync(); Process.Start(new ProcessStartInfo("http://127.0.0.1:51789/") { UseShellExecute = true }); };
        FormClosed += (_, _) => { serverCts?.Cancel(); listener?.Close(); };
    }

    string ScriptPath(string name) => Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "godaddy", name));

    void RunScript(string name)
    {
        try
        {
            var script = ScriptPath(name);
            if (!File.Exists(script)) { MessageBox.Show($"Script not found: {script}", "MilanSetu Backup Manager"); return; }
            Directory.CreateDirectory(Path.GetDirectoryName(config.Text)!);
            var psi = new ProcessStartInfo("powershell.exe", $"-NoProfile -ExecutionPolicy Bypass -File \"{script}\" -ConfigPath \"{config.Text}\"") { UseShellExecute = true };
            Process.Start(psi);
            status.Text = name + " started.";
        }
        catch (Exception ex) { status.Text = "Failed: " + ex.Message; }
    }

    void OpenBackupFolder()
    {
        try
        {
            using var doc = JsonDocument.Parse(File.ReadAllText(config.Text));
            var root = doc.RootElement.GetProperty("LocalBackupRoot").GetString()!;
            Directory.CreateDirectory(root);
            Process.Start(new ProcessStartInfo("explorer.exe", root) { UseShellExecute = true });
        }
        catch (Exception ex) { MessageBox.Show(ex.Message, "MilanSetu Backup Manager"); }
    }

    async Task StartWebAsync()
    {
        if (listener is { IsListening: true }) return;
        listener = new HttpListener(); listener.Prefixes.Add("http://127.0.0.1:51789/"); listener.Start();
        serverCts = new CancellationTokenSource();
        _ = Task.Run(async () =>
        {
            while (!serverCts.IsCancellationRequested && listener.IsListening)
            {
                try { var ctx = await listener.GetContextAsync(); _ = Task.Run(() => Handle(ctx)); }
                catch { break; }
            }
        });
        status.Text = "Local web dashboard running at http://127.0.0.1:51789/";
    }

    void Handle(HttpListenerContext ctx)
    {
        try
        {
            if (ctx.Request.Url?.AbsolutePath == "/api/status")
            {
                var data = new { configExists = File.Exists(config.Text), configPath = config.Text, now = DateTimeOffset.Now };
                Write(ctx, JsonSerializer.Serialize(data), "application/json"); return;
            }
            Write(ctx, WebPage(), "text/html; charset=utf-8");
        }
        catch { try { ctx.Response.StatusCode = 500; ctx.Response.Close(); } catch { } }
    }

    static void Write(HttpListenerContext ctx, string body, string contentType)
    {
        var bytes = Encoding.UTF8.GetBytes(body); ctx.Response.ContentType = contentType; ctx.Response.ContentLength64 = bytes.Length;
        using var s = ctx.Response.OutputStream; s.Write(bytes, 0, bytes.Length);
    }

    string WebPage() => """
<!doctype html><html><head><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1">
<title>MilanSetu Backup Manager</title><style>
body{font-family:Segoe UI,Arial;margin:0;background:#f6f7f9;color:#20242a}.wrap{max-width:760px;margin:40px auto;padding:24px}.card{background:#fff;border:1px solid #e2e5e9;border-radius:16px;padding:24px;box-shadow:0 5px 20px #0000000b}h1{margin-top:0}button{padding:11px 16px;border:0;border-radius:10px;cursor:pointer;margin:5px}#status{margin-top:16px;padding:12px;background:#f0f3f6;border-radius:10px}
</style></head><body><div class="wrap"><div class="card"><h1>MilanSetu Backup Manager</h1>
<p>Local-only dashboard. It does not expose your backup files or database credentials to the internet.</p>
<button onclick="location.reload()">Refresh status</button><div id="status">Checking…</div></div></div>
<script>fetch('/api/status').then(r=>r.json()).then(x=>document.getElementById('status').textContent='Config: '+(x.configExists?'Ready':'Not configured')+' · '+x.configPath+' · '+x.now).catch(()=>document.getElementById('status').textContent='Manager is not running.');</script>
</body></html>
""";
}
