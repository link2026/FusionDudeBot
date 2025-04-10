using PKHeX.Core;
using SysBot.Base;
using SysBot.Pokemon.Z3;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Windows.Forms;
using SysBot.Pokemon.Helpers;
using System.Drawing;
using SysBot.Pokemon.WinForms.Properties;
using Microsoft.Extensions.DependencyInjection;

namespace SysBot.Pokemon.WinForms;

public sealed partial class Main : Form
{
    private readonly List<PokeBotState> Bots = [];


    private IPokeBotRunner RunningEnvironment { get; set; }
    private ProgramConfig Config { get; set; }
    public static bool IsUpdating { get; set; } = false;

    private bool _isFormLoading = true;
    public Main()
    {
        InitializeComponent();
        comboBox1.SelectedIndexChanged += new EventHandler(comboBox1_SelectedIndexChanged);
        this.Load += async (sender, e) => await InitializeAsync();

    }

    private async Task InitializeAsync()
    {
        if (IsUpdating)
            return;
        PokeTradeBotSWSH.SeedChecker = new Z3SeedSearchHandler<PK8>();


        // Update checker
        UpdateChecker updateChecker = new UpdateChecker();
        await UpdateChecker.CheckForUpdatesAsync();


        if (File.Exists(Program.ConfigPath))
        {
            var lines = File.ReadAllText(Program.ConfigPath);
            Config = JsonSerializer.Deserialize(lines, ProgramConfigContext.Default.ProgramConfig) ?? new ProgramConfig();
            LogConfig.MaxArchiveFiles = Config.Hub.MaxArchiveFiles;
            LogConfig.LoggingEnabled = Config.Hub.LoggingEnabled;
            comboBox1.SelectedValue = (int)Config.Mode;
            RunningEnvironment = GetRunner(Config);
            foreach (var bot in Config.Bots)
            {
                bot.Initialize();
                AddBot(bot);
            }
        }
        else
        {
            Config = new ProgramConfig();
            RunningEnvironment = GetRunner(Config);
            Config.Hub.Folder.CreateDefaults(Program.WorkingDirectory);
        }

        RTB_Logs.MaxLength = 32_767; // character length
        LoadControls();
        Text = $"{(string.IsNullOrEmpty(Config.Hub.BotName) ? "FusionDudeBot |" : Config.Hub.BotName)} {TradeBot.Version} | Mode: {Config.Mode}";
        Task.Run(BotMonitor);
        InitUtil.InitializeStubs(Config.Mode);
        _isFormLoading = false;
        UpdateBackgroundImage(Config.Mode);
    }

    private static IPokeBotRunner GetRunner(ProgramConfig cfg) => cfg.Mode switch
    {
        ProgramMode.SWSH => new PokeBotRunnerImpl<PK8>(cfg.Hub, new BotFactory8SWSH()),
        ProgramMode.BDSP => new PokeBotRunnerImpl<PB8>(cfg.Hub, new BotFactory8BS()),
        ProgramMode.LA => new PokeBotRunnerImpl<PA8>(cfg.Hub, new BotFactory8LA()),
        ProgramMode.SV => new PokeBotRunnerImpl<PK9>(cfg.Hub, new BotFactory9SV()),
        ProgramMode.LGPE => new PokeBotRunnerImpl<PB7>(cfg.Hub, new BotFactory7LGPE()),
        _ => throw new IndexOutOfRangeException("Unsupported mode."),
    };

    private async Task BotMonitor()
    {
        while (!Disposing)
        {
            try
            {
                foreach (var c in FLP_Bots.Controls.OfType<BotController>())
                    c.ReadState();
            }
            catch
            {
                // Updating the collection by adding/removing bots will change the iterator
                // Can try a for-loop or ToArray, but those still don't prevent concurrent mutations of the array.
                // Just try, and if failed, ignore. Next loop will be fine. Locks on the collection are kinda overkill, since this task is not critical.
            }
            await Task.Delay(2_000).ConfigureAwait(false);
        }
    }

    private void LoadControls()
    {
        MinimumSize = Size;
        PG_Hub.SelectedObject = RunningEnvironment.Config;

        var routines = ((PokeRoutineType[])Enum.GetValues(typeof(PokeRoutineType))).Where(z => RunningEnvironment.SupportsRoutine(z));
        var list = routines.Select(z => new ComboItem(z.ToString(), (int)z)).ToArray();
        CB_Routine.DisplayMember = nameof(ComboItem.Text);
        CB_Routine.ValueMember = nameof(ComboItem.Value);
        CB_Routine.DataSource = list;
        CB_Routine.SelectedValue = (int)PokeRoutineType.FlexTrade; // default option

        var protocols = (SwitchProtocol[])Enum.GetValues(typeof(SwitchProtocol));
        var listP = protocols.Select(z => new ComboItem(z.ToString(), (int)z)).ToArray();
        CB_Protocol.DisplayMember = nameof(ComboItem.Text);
        CB_Protocol.ValueMember = nameof(ComboItem.Value);
        CB_Protocol.DataSource = listP;
        CB_Protocol.SelectedIndex = (int)SwitchProtocol.WiFi; // default option
                                                              // Populate the game mode dropdown
        var gameModes = Enum.GetValues(typeof(ProgramMode))
            .Cast<ProgramMode>()
            .Where(m => m != ProgramMode.None) // Exclude the 'None' value
            .Select(mode => new { Text = mode.ToString(), Value = (int)mode })
            .ToList();

        comboBox1.DisplayMember = "Text";
        comboBox1.ValueMember = "Value";
        comboBox1.DataSource = gameModes;

        // Set the current mode as selected in the dropdown
        comboBox1.SelectedValue = (int)Config.Mode;

        comboBox2.Items.Add("Light");
        comboBox2.Items.Add("Dark");
        comboBox2.Items.Add("Poke");
        comboBox2.Items.Add("Gengar");
        comboBox2.Items.Add("Zeraora");
        comboBox2.Items.Add("Shiny Zeraora");
        comboBox2.Items.Add("Green");
        comboBox2.Items.Add("Blue");
        comboBox2.Items.Add("Akatsuki");
        comboBox2.Items.Add("Naruto");
        comboBox2.Items.Add("Shiny Mewtwo");
        comboBox2.Items.Add("Shiny Umbreon");
        comboBox2.Items.Add("Scarlet");
        comboBox2.Items.Add("Violet");
        comboBox2.Items.Add("Black & White");
        comboBox2.Items.Add("Messy Colors");
        comboBox2.Items.Add("Pitch Black");

        // Load the current theme from configuration and set it in the comboBox2
        string theme = Config.Hub.ThemeOption;
        if (string.IsNullOrEmpty(theme) || !comboBox2.Items.Contains(theme))
        {
            comboBox2.SelectedIndex = 0;  // Set default selection to Light Mode if ThemeOption is empty or invalid
        }
        else
        {
            comboBox2.SelectedItem = theme;  // Set the selected item in the combo box based on ThemeOption
        }
        switch (theme)
        {
            case "Dark":
                ApplyDarkTheme();
                break;
            case "Light":
                ApplyLightTheme();
                break;
            case "Poke":
                ApplyPokemonTheme();
                break;
            case "Gengar":
                ApplyGengarTheme();
                break;
            case "Zeraora":
                ApplyZeraoraTheme();
                break;
            case "Shiny Zeraora":
                ApplyShinyZeraoraTheme();
                break;
            case "Green":
                ApplyGreenTheme();
                break;
            case "Blue":
                ApplyBlueTheme();
                break;
            case "Akatsuki":
                ApplyAkatsukiTheme();
                break;
            case "Naruto":
                ApplyNarutoTheme();
                break;
            case "Shiny Mewtwo":
                ApplyShinyMewtwoTheme();
                break;
            case "Shiny Umbreon":
                ApplyShinyUmbreonTheme();
                break;
            case "Scarlet":
                ApplyPokemonScarletTheme();
                break;
            case "Violet":
                ApplyPokemonVioletTheme();
                break;
            case "Black & White":
                ApplyBlackAndWhiteTheme();
                break;
            case "Messy Colors":
                ApplyRainbowTheme();
                break;
            case "Pitch Black":
                ApplyPitchBlackTheme();
                break;
            default:
                ApplyLightTheme();
                break;
        }

        LogUtil.Forwarders.Add(new TextBoxForwarder(RTB_Logs));
    }

    private ProgramConfig GetCurrentConfiguration()
    {

        Config.Bots = Bots.ToArray();
        return Config;
    }

    private void Main_FormClosing(object sender, FormClosingEventArgs e)
    {
        if (IsUpdating)
        {
            return;
        }
        SaveCurrentConfig();
        var bots = RunningEnvironment;
        if (!bots.IsRunning)
            return;

        async Task WaitUntilNotRunning()
        {
            while (bots.IsRunning)
                await Task.Delay(10).ConfigureAwait(false);
        }

        // Try to let all bots hard-stop before ending execution of the entire program.
        WindowState = FormWindowState.Minimized;
        ShowInTaskbar = false;
        bots.StopAll();
        Task.WhenAny(WaitUntilNotRunning(), Task.Delay(5_000)).ConfigureAwait(true).GetAwaiter().GetResult();
    }

    private void SaveCurrentConfig()
    {
        var cfg = GetCurrentConfiguration();
        var lines = JsonSerializer.Serialize(cfg, ProgramConfigContext.Default.ProgramConfig);
        File.WriteAllText(Program.ConfigPath, lines);
    }

    [JsonSerializable(typeof(ProgramConfig))]
    [JsonSourceGenerationOptions(WriteIndented = true, DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
    public sealed partial class ProgramConfigContext : JsonSerializerContext;
    private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
    {
        if (_isFormLoading) return; // Check to avoid processing during form loading

        if (comboBox1.SelectedValue is int selectedValue)
        {
            ProgramMode newMode = (ProgramMode)selectedValue;
            Config.Mode = newMode;

            SaveCurrentConfig();
            UpdateRunnerAndUI();

            UpdateBackgroundImage(newMode);
        }
    }

    private void UpdateRunnerAndUI()
    {
        RunningEnvironment = GetRunner(Config);
        Text = $"{(string.IsNullOrEmpty(Config.Hub.BotName) ? "FusionDudeBot |" : Config.Hub.BotName)} {TradeBot.Version} | Mode: {Config.Mode}";
    }

    private void B_Start_Click(object sender, EventArgs e)
    {
        SaveCurrentConfig();

        LogUtil.LogInfo("Starting all bots...", "Form");
        RunningEnvironment.InitializeStart();
        SendAll(BotControlCommand.Start);
        Tab_Logs.Select();

        if (Bots.Count == 0)
            WinFormsUtil.Alert("No bots configured, but all supporting services have been started.");
    }


    private void B_RebootStop_Click(object sender, EventArgs e)
    {
        B_Stop_Click(sender, e);
        Task.Run(async () =>
        {
            await Task.Delay(3_500).ConfigureAwait(false);
            SaveCurrentConfig();
            LogUtil.LogInfo("Restarting all the consoles...", "Form");
            RunningEnvironment.InitializeStart();
            SendAll(BotControlCommand.RebootAndStop);
            await Task.Delay(5_000).ConfigureAwait(false); // Add a delay before restarting the bot
            SendAll(BotControlCommand.Start); // Start the bot after the delay
            Tab_Logs.Select();
            if (Bots.Count == 0)
                WinFormsUtil.Alert("No bots configured, but all supporting services have been issued the reboot command.");
        });
    }


    private void UpdateBackgroundImage(ProgramMode mode)
    {
        switch (mode)
        {
            case ProgramMode.SV:
                FLP_Bots.BackgroundImage = Resources.sv_mode_image;
                break;
            case ProgramMode.SWSH:
                FLP_Bots.BackgroundImage = Resources.swsh_mode_image;
                break;
            case ProgramMode.BDSP:
                FLP_Bots.BackgroundImage = Resources.bdsp_mode_image;
                break;
            case ProgramMode.LA:
                FLP_Bots.BackgroundImage = Resources.pla_mode_image;
                break;
            case ProgramMode.LGPE:
                FLP_Bots.BackgroundImage = Resources.lgpe_mode_image;
                break;
            default:
                FLP_Bots.BackgroundImage = null;
                break;
        }
        FLP_Bots.BackgroundImageLayout = ImageLayout.Center;
    }

    private void SendAll(BotControlCommand cmd)
    {
        foreach (var c in FLP_Bots.Controls.OfType<BotController>())
            c.SendCommand(cmd);

        //    EchoUtil.Echo($"All bots have been issued a command to {cmd}.");
    }

    private void B_Stop_Click(object sender, EventArgs e)
    {
        var env = RunningEnvironment;
        if (!env.IsRunning && (ModifierKeys & Keys.Alt) == 0)
        {
            WinFormsUtil.Alert("Nothing is currently running.");
            return;
        }

        var cmd = BotControlCommand.Stop;

        if ((ModifierKeys & Keys.Control) != 0 || (ModifierKeys & Keys.Shift) != 0) // either, because remembering which can be hard
        {
            if (env.IsRunning)
            {
                WinFormsUtil.Alert("Commanding all bots to Idle.", "Press Stop (without a modifier key) to hard-stop and unlock control, or press Stop with the modifier key again to resume.");
                cmd = BotControlCommand.Idle;
            }
            else
            {
                WinFormsUtil.Alert("Commanding all bots to resume their original task.", "Press Stop (without a modifier key) to hard-stop and unlock control.");
                cmd = BotControlCommand.Resume;
            }
        }
        else
        {
            env.StopAll();
        }
        SendAll(cmd);
    }

    private void B_New_Click(object sender, EventArgs e)
    {
        var cfg = CreateNewBotConfig();
        if (!AddBot(cfg))
        {
            WinFormsUtil.Alert("Unable to add bot; ensure details are valid and not duplicate with an already existing bot.");
            return;
        }
        System.Media.SystemSounds.Asterisk.Play();
    }

    private async void Updater_Click(object sender, EventArgs e)
    {
        var (updateAvailable, updateRequired, newVersion) = await UpdateChecker.CheckForUpdatesAsync();
        if (!updateAvailable)
        {
            var result = MessageBox.Show(
            "You are on the latest version. Would you like to re-download the current version?",
            "Update Check",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Question);
            if (result == DialogResult.Yes)
            {
                UpdateForm updateForm = new UpdateForm(updateRequired, newVersion, updateAvailable: false);
                updateForm.ShowDialog();
            }
        }
        else
        {
            UpdateForm updateForm = new UpdateForm(updateRequired, newVersion, updateAvailable: true);
            updateForm.ShowDialog();
        }
    }

    private bool AddBot(PokeBotState cfg)
    {
        if (!cfg.IsValid())
            return false;

        if (Bots.Any(z => z.Connection.Equals(cfg.Connection)))
            return false;

        PokeRoutineExecutorBase newBot;
        try
        {
            Console.WriteLine($"Current Mode ({Config.Mode}) does not support this type of bot ({cfg.CurrentRoutineType}).");
            newBot = RunningEnvironment.CreateBotFromConfig(cfg);
        }
        catch
        {
            return false;
        }

        try
        {
            RunningEnvironment.Add(newBot);
        }
        catch (ArgumentException ex)
        {
            WinFormsUtil.Error(ex.Message);
            return false;
        }

        AddBotControl(cfg);
        Bots.Add(cfg);
        return true;
    }

    private void AddBotControl(PokeBotState cfg)
    {
        var row = new BotController { Width = FLP_Bots.Width };
        row.Initialize(RunningEnvironment, cfg);
        FLP_Bots.Controls.Add(row);
        FLP_Bots.SetFlowBreak(row, true);
        row.Click += (s, e) =>
        {
            var details = cfg.Connection;
            TB_IP.Text = details.IP;
            NUD_Port.Value = details.Port;
            CB_Protocol.SelectedIndex = (int)details.Protocol;
            CB_Routine.SelectedValue = (int)cfg.InitialRoutine;
        };

        row.Remove += (s, e) =>
        {
            Bots.Remove(row.State);
            RunningEnvironment.Remove(row.State, !RunningEnvironment.Config.SkipConsoleBotCreation);
            FLP_Bots.Controls.Remove(row);
        };
    }

    private PokeBotState CreateNewBotConfig()
    {
        var ip = TB_IP.Text;
        var port = (int)NUD_Port.Value;
        var cfg = BotConfigUtil.GetConfig<SwitchConnectionConfig>(ip, port);
        cfg.Protocol = (SwitchProtocol)WinFormsUtil.GetIndex(CB_Protocol);

        var pk = new PokeBotState { Connection = cfg };
        var type = (PokeRoutineType)WinFormsUtil.GetIndex(CB_Routine);
        pk.Initialize(type);
        return pk;
    }

    private void FLP_Bots_Resize(object sender, EventArgs e)
    {
        foreach (var c in FLP_Bots.Controls.OfType<BotController>())
            c.Width = FLP_Bots.Width;
    }

    private void CB_Protocol_SelectedIndexChanged(object sender, EventArgs e)
    {
        TB_IP.Visible = CB_Protocol.SelectedIndex == 0;
    }

    private void comboBox2_SelectedIndexChanged(object sender, EventArgs e)
    {
        if (sender is ComboBox comboBox)
        {
            string selectedTheme = comboBox.SelectedItem.ToString();
            Config.Hub.ThemeOption = selectedTheme;  // Save the selected theme to the config
            SaveCurrentConfig();  // Save the config to file

            switch (selectedTheme)
            {
                case "Light":
                    ApplyLightTheme();
                    break;
                case "Dark":
                    ApplyDarkTheme();
                    break;
                case "Poke":
                    ApplyPokemonTheme();
                    break;
                case "Gengar":
                    ApplyGengarTheme();
                    break;
                case "Zeraora":
                    ApplyZeraoraTheme();
                    break;
                case "Shiny Zeraora":
                    ApplyShinyZeraoraTheme();
                    break;
                case "Green":
                    ApplyGreenTheme();
                    break;
                case "Blue":
                    ApplyBlueTheme();
                    break;
                case "Akatsuki":
                    ApplyAkatsukiTheme();
                    break;
                case "Naruto":
                    ApplyNarutoTheme();
                    break;
                case "Shiny Mewtwo":
                    ApplyShinyMewtwoTheme();
                    break;
                case "Shiny Umbreon":
                    ApplyShinyUmbreonTheme();
                    break;
                case "Scarlet":
                    ApplyPokemonScarletTheme();
                    break;
                case "Violet":
                    ApplyPokemonVioletTheme();
                    break;
                case "Black & White":
                    ApplyBlackAndWhiteTheme();
                    break;
                case "Messy Colors":
                    ApplyRainbowTheme();
                    break;
                case "Pitch Black":
                    ApplyPitchBlackTheme();
                    break;
                default:
                    ApplyLightTheme();
                    break;
            }
        }
    }

    private void ApplyZeraoraTheme()
    {
        // Define Zeraora-theme colors
        Color SoftGold = Color.FromArgb(218, 165, 32);        // Soft gold color
        Color SkyBlue = Color.FromArgb(0, 134, 213);          // Sky blue color (RGB: 0, 134, 213)
        Color MediumDarkGray = Color.FromArgb(80, 80, 80);    // Medium-dark gray color
        Color White = Color.White;                            // White color

        // Set the background color of the Hub form
        this.BackColor = MediumDarkGray;                     // Medium-dark gray for the background

        // Set the foreground color of the main status form
        this.ForeColor = White;                              // White text color for the status form

        // Set the background color of the tab control
        TC_Main.BackColor = MediumDarkGray;                  // Medium-dark gray for tab control

        // Set the background color of each tab page
        foreach (TabPage page in TC_Main.TabPages)
        {
            page.BackColor = SoftGold;                       // Soft gold for each tab page
        }

        // Set the background color of the Hub
        PG_Hub.BackColor = MediumDarkGray;                   // Medium-dark gray for Hub
        PG_Hub.LineColor = MediumDarkGray;                   // Line color for Hub
        PG_Hub.CategoryForeColor = White;                   // White font color for category text
        PG_Hub.CategorySplitterColor = MediumDarkGray;       // Category splitter color
        PG_Hub.HelpBackColor = MediumDarkGray;               // Help background color
        PG_Hub.HelpForeColor = White;                        // White help text color
        PG_Hub.ViewBackColor = MediumDarkGray;
        PG_Hub.ViewForeColor = White;

        // Set the colors of the Log tab
        RTB_Logs.BackColor = MediumDarkGray;                // Medium-dark gray for log background
        RTB_Logs.ForeColor = White;                         // White text color for logs

        // Set the colors of the IP form
        TB_IP.BackColor = SkyBlue;                          // Sky blue for IP form box background
        TB_IP.ForeColor = White;                             // White text color for IP form box

        CB_Routine.BackColor = MediumDarkGray;              // Medium-dark gray for combo box background
        CB_Routine.ForeColor = White;                        // White text color for combo box

        NUD_Port.BackColor = SkyBlue;                       // Sky blue for Port box background
        NUD_Port.ForeColor = White;                          // White text color for Port box

        B_New.BackColor = SkyBlue;                          // Sky blue for button background
        B_New.ForeColor = White;                            // White text color for button

        FLP_Bots.BackColor = MediumDarkGray;                // Medium-dark gray for panel behind trade type and status information

        CB_Protocol.BackColor = MediumDarkGray;             // Medium-dark gray for protocol combo box background
        CB_Protocol.ForeColor = White;                       // White text color for protocol combo box

        comboBox1.BackColor = MediumDarkGray;               // Medium-dark gray for combo box background
        comboBox1.ForeColor = White;                         // White text color for combo box

        B_Stop.BackColor = SkyBlue;                         // Sky blue for STOP button background
        B_Stop.ForeColor = White;                            // White text color for STOP button font

        B_Start.BackColor = SkyBlue;                        // Sky blue for START button background
        B_Start.ForeColor = White;                           // White text color for START button font

        B_RebootStop.BackColor = SkyBlue;                   // Sky blue for REBOOT STOP button background
        B_RebootStop.ForeColor = White;                      // White text color for REBOOT STOP button font

        updater.BackColor = SkyBlue;                        // Sky blue for updater background
        updater.ForeColor = White;                           // White text color for updater font
    }

    private void ApplyShinyZeraoraTheme()
    {
        // Define shiny Zeraora-theme colors
        Color SoftMatteWhite = Color.FromArgb(240, 240, 240);      // Soft matte white color
        Color SoftMatteTurquoise = Color.FromArgb(64, 224, 208);   // Soft matte turquoise color (more green)
        Color SoftMatteTeal = Color.FromArgb(0, 128, 128);        // Soft matte teal color
        Color SoftMatteGold = Color.FromArgb(255, 215, 0);         // Bolder brighter gold color
        Color SoftMatteText = Color.Black;                         // Soft matte text color
        Color SoftMatteGrey = Color.FromArgb(180, 180, 180);      // Soft matte grey color

        // Set the background color of the Hub form
        this.BackColor = SoftMatteWhite;

        // Set the foreground color of the main status form
        this.ForeColor = SoftMatteText;

        // Set the background color of the tab control
        TC_Main.BackColor = SoftMatteWhite;

        // Set the background color of each tab page
        foreach (TabPage page in TC_Main.TabPages)
        {
            page.BackColor = SoftMatteGrey;
        }

        // Set the background color of the Hub
        PG_Hub.BackColor = SoftMatteWhite;
        PG_Hub.LineColor = SoftMatteWhite;
        PG_Hub.CategoryForeColor = SoftMatteText;
        PG_Hub.CategorySplitterColor = SoftMatteWhite;
        PG_Hub.HelpBackColor = SoftMatteWhite;
        PG_Hub.HelpForeColor = SoftMatteText;
        PG_Hub.ViewBackColor = SoftMatteWhite;
        PG_Hub.ViewForeColor = SoftMatteText;

        // Set the colors of the Log tab
        RTB_Logs.BackColor = SoftMatteWhite;
        RTB_Logs.ForeColor = SoftMatteText;

        // Set the colors of the IP form
        TB_IP.BackColor = SoftMatteWhite;
        TB_IP.ForeColor = SoftMatteText;

        CB_Routine.BackColor = SoftMatteWhite;
        CB_Routine.ForeColor = SoftMatteText;

        NUD_Port.BackColor = SoftMatteWhite;
        NUD_Port.ForeColor = SoftMatteText;

        B_New.BackColor = SoftMatteWhite;
        B_New.ForeColor = SoftMatteText;

        FLP_Bots.BackColor = SoftMatteWhite;

        CB_Protocol.BackColor = SoftMatteWhite;
        CB_Protocol.ForeColor = SoftMatteText;

        comboBox1.BackColor = SoftMatteWhite;
        comboBox1.ForeColor = SoftMatteText;

        B_Stop.BackColor = SoftMatteTeal;
        B_Stop.ForeColor = SoftMatteWhite;

        B_Start.BackColor = SoftMatteTeal;
        B_Start.ForeColor = SoftMatteWhite;

        B_RebootStop.BackColor = SoftMatteGold;
        B_RebootStop.ForeColor = SoftMatteText;

        updater.BackColor = SoftMatteGold;
        updater.ForeColor = SoftMatteText;
    }

    private void ApplyGengarTheme()
    {
        Color GengarPurple = Color.FromArgb(88, 88, 120);  // A muted purple, the main color of Gengar
        Color DarkShadow = Color.FromArgb(40, 40, 60);     // A deeper shade for shadowing and contrast
        Color GhostlyGrey = Color.FromArgb(200, 200, 215); // A soft grey for text and borders
        Color HauntingBlue = Color.FromArgb(80, 80, 160);  // A haunting blue for accenting and highlights
        Color MidnightBlack = Color.FromArgb(25, 25, 35);  // A near-black for the darkest areas
        Color HauntingShadows = Color.FromArgb(68, 68, 119);  // A haunting blue in dark shadow

        // Set the background color of the form
        this.BackColor = MidnightBlack;

        // Set the foreground color of the form (text color)
        this.ForeColor = GhostlyGrey;

        // Set the background color of the tab control
        TC_Main.BackColor = GengarPurple;

        // Set the background color of each tab page
        foreach (TabPage page in TC_Main.TabPages)
        {
            page.BackColor = DarkShadow;
        }

        // Set the background color of the property grid
        PG_Hub.BackColor = DarkShadow;
        PG_Hub.LineColor = HauntingBlue;
        PG_Hub.CategoryForeColor = GhostlyGrey;
        PG_Hub.CategorySplitterColor = HauntingBlue;
        PG_Hub.HelpBackColor = DarkShadow;
        PG_Hub.HelpForeColor = GhostlyGrey;
        PG_Hub.ViewBackColor = DarkShadow;
        PG_Hub.ViewForeColor = GhostlyGrey;

        // Set the background color of the rich text box
        RTB_Logs.BackColor = MidnightBlack;
        RTB_Logs.ForeColor = GhostlyGrey;

        // Set colors for other controls
        TB_IP.BackColor = GengarPurple;
        TB_IP.ForeColor = GhostlyGrey;

        CB_Routine.BackColor = GengarPurple;
        CB_Routine.ForeColor = GhostlyGrey;

        NUD_Port.BackColor = GengarPurple;
        NUD_Port.ForeColor = GhostlyGrey;

        B_New.BackColor = HauntingBlue;
        B_New.ForeColor = GhostlyGrey;

        FLP_Bots.BackColor = DarkShadow;

        CB_Protocol.BackColor = GengarPurple;
        CB_Protocol.ForeColor = GhostlyGrey;

        comboBox1.BackColor = GengarPurple;
        comboBox1.ForeColor = GhostlyGrey;

        B_Stop.BackColor = HauntingBlue;
        B_Stop.ForeColor = GhostlyGrey;

        B_Start.BackColor = HauntingBlue;
        B_Start.ForeColor = GhostlyGrey;

        B_RebootStop.BackColor = HauntingShadows;
        B_RebootStop.ForeColor = GhostlyGrey;

        updater.BackColor = HauntingShadows;
        updater.ForeColor = GhostlyGrey;

    }

    private void ApplyLightTheme()
    {
        // Define the color palette
        Color SoftBlue = Color.FromArgb(235, 245, 251);
        Color GentleGrey = Color.FromArgb(245, 245, 245);
        Color DarkBlue = Color.FromArgb(26, 13, 171);
        Color HarderSoftBlue = Color.FromArgb(240, 245, 255);

        // Set the background color of the form
        this.BackColor = GentleGrey;

        // Set the foreground color of the form (text color)
        this.ForeColor = DarkBlue;

        // Set the background color of the tab control
        TC_Main.BackColor = SoftBlue;

        // Set the background color of each tab page
        foreach (TabPage page in TC_Main.TabPages)
        {
            page.BackColor = GentleGrey;
        }

        // Set the background color of the property grid
        PG_Hub.BackColor = GentleGrey;
        PG_Hub.LineColor = SoftBlue;
        PG_Hub.CategoryForeColor = DarkBlue;
        PG_Hub.CategorySplitterColor = SoftBlue;
        PG_Hub.HelpBackColor = GentleGrey;
        PG_Hub.HelpForeColor = DarkBlue;
        PG_Hub.ViewBackColor = GentleGrey;
        PG_Hub.ViewForeColor = DarkBlue;

        // Set the background color of the rich text box
        RTB_Logs.BackColor = Color.White;
        RTB_Logs.ForeColor = DarkBlue;

        // Set colors for other controls
        TB_IP.BackColor = Color.White;
        TB_IP.ForeColor = DarkBlue;

        CB_Routine.BackColor = Color.White;
        CB_Routine.ForeColor = DarkBlue;

        NUD_Port.BackColor = Color.White;
        NUD_Port.ForeColor = DarkBlue;

        B_New.BackColor = SoftBlue;
        B_New.ForeColor = DarkBlue;

        FLP_Bots.BackColor = GentleGrey;

        CB_Protocol.BackColor = Color.White;
        CB_Protocol.ForeColor = DarkBlue;

        comboBox1.BackColor = Color.White;
        comboBox1.ForeColor = DarkBlue;

        B_Stop.BackColor = SoftBlue;
        B_Stop.ForeColor = DarkBlue;

        B_Start.BackColor = SoftBlue;
        B_Start.ForeColor = DarkBlue;

        B_RebootStop.BackColor = HarderSoftBlue;
        B_RebootStop.ForeColor = DarkBlue;

        updater.BackColor = HarderSoftBlue;
        updater.ForeColor = DarkBlue;

    }

    private void ApplyPokemonTheme()
    {
        // Define Poke-theme colors
        Color PokeRed = Color.FromArgb(206, 12, 30);      // A classic red tone reminiscent of the Pokeball
        Color DarkPokeRed = Color.FromArgb(164, 10, 24);  // A darker shade of the PokeRed for contrast and depth
        Color SleekGrey = Color.FromArgb(46, 49, 54);     // A sleek grey for background and contrast
        Color SoftWhite = Color.FromArgb(230, 230, 230);  // A soft white for text and borders
        Color MidnightBlack = Color.FromArgb(18, 19, 20); // A near-black for darker elements and depth
        Color PokeRedShadow = Color.FromArgb(183, 1, 19); // A classic red tone reminiscent of the Pokeball in a dark shadow

        // Set the background color of the form
        this.BackColor = SleekGrey;

        // Set the foreground color of the form (text color)
        this.ForeColor = SoftWhite;

        // Set the background color of the tab control
        TC_Main.BackColor = DarkPokeRed;

        // Set the background color of each tab page
        foreach (TabPage page in TC_Main.TabPages)
        {
            page.BackColor = SleekGrey;
        }

        // Set the background color of the property grid
        PG_Hub.BackColor = SleekGrey;
        PG_Hub.LineColor = DarkPokeRed;
        PG_Hub.CategoryForeColor = SoftWhite;
        PG_Hub.CategorySplitterColor = DarkPokeRed;
        PG_Hub.HelpBackColor = SleekGrey;
        PG_Hub.HelpForeColor = SoftWhite;
        PG_Hub.ViewBackColor = SleekGrey;
        PG_Hub.ViewForeColor = SoftWhite;

        // Set the background color of the rich text box
        RTB_Logs.BackColor = MidnightBlack;
        RTB_Logs.ForeColor = SoftWhite;

        // Set colors for other controls
        TB_IP.BackColor = DarkPokeRed;
        TB_IP.ForeColor = SoftWhite;

        CB_Routine.BackColor = DarkPokeRed;
        CB_Routine.ForeColor = SoftWhite;

        NUD_Port.BackColor = DarkPokeRed;
        NUD_Port.ForeColor = SoftWhite;

        B_New.BackColor = PokeRed;
        B_New.ForeColor = SoftWhite;

        FLP_Bots.BackColor = SleekGrey;

        CB_Protocol.BackColor = DarkPokeRed;
        CB_Protocol.ForeColor = SoftWhite;

        comboBox1.BackColor = DarkPokeRed;
        comboBox1.ForeColor = SoftWhite;

        B_Stop.BackColor = PokeRed;
        B_Stop.ForeColor = SoftWhite;

        B_Start.BackColor = PokeRed;
        B_Start.ForeColor = SoftWhite;

        B_RebootStop.BackColor = PokeRedShadow;
        B_RebootStop.ForeColor = SoftWhite;

        updater.BackColor = PokeRedShadow;
        updater.ForeColor = SoftWhite;

    }

    private void ApplyDarkTheme()
    {
        // Define the dark theme colors
        Color DarkRed = Color.FromArgb(90, 0, 0);
        Color DarkGrey = Color.FromArgb(30, 30, 30);
        Color LightGrey = Color.FromArgb(60, 60, 60);
        Color SoftWhite = Color.FromArgb(245, 245, 245);
        Color DarkRedShadow = Color.FromArgb(65, 2, 2);

        // Set the background color of the form
        this.BackColor = DarkGrey;

        // Set the foreground color of the form (text color)
        this.ForeColor = SoftWhite;

        // Set the background color of the tab control
        TC_Main.BackColor = LightGrey;

        // Set the background color of each tab page
        foreach (TabPage page in TC_Main.TabPages)
        {
            page.BackColor = DarkGrey;
        }

        // Set the background color of the property grid
        PG_Hub.BackColor = DarkGrey;
        PG_Hub.LineColor = LightGrey;
        PG_Hub.CategoryForeColor = SoftWhite;
        PG_Hub.CategorySplitterColor = LightGrey;
        PG_Hub.HelpBackColor = DarkGrey;
        PG_Hub.HelpForeColor = SoftWhite;
        PG_Hub.ViewBackColor = DarkGrey;
        PG_Hub.ViewForeColor = SoftWhite;

        // Set the background color of the rich text box
        RTB_Logs.BackColor = DarkGrey;
        RTB_Logs.ForeColor = SoftWhite;

        // Set colors for other controls
        TB_IP.BackColor = LightGrey;
        TB_IP.ForeColor = SoftWhite;

        CB_Routine.BackColor = LightGrey;
        CB_Routine.ForeColor = SoftWhite;

        NUD_Port.BackColor = LightGrey;
        NUD_Port.ForeColor = SoftWhite;

        B_New.BackColor = DarkRed;
        B_New.ForeColor = SoftWhite;

        FLP_Bots.BackColor = DarkGrey;

        CB_Protocol.BackColor = LightGrey;
        CB_Protocol.ForeColor = SoftWhite;

        comboBox1.BackColor = LightGrey;
        comboBox1.ForeColor = SoftWhite;

        B_Stop.BackColor = DarkRed;
        B_Stop.ForeColor = SoftWhite;

        B_Start.BackColor = DarkRed;
        B_Start.ForeColor = SoftWhite;

        B_RebootStop.BackColor = DarkRedShadow;
        B_RebootStop.ForeColor = SoftWhite;

        updater.BackColor = DarkRedShadow;
        updater.ForeColor = SoftWhite;

    }


    private void ApplyGreenTheme()
    {
        // Define Green Mode theme colors
        Color DarkGreenBG = Color.FromArgb(50, 64, 52);         // A muted grayish-green
        Color LightTurqoise = Color.FromArgb(91, 156, 101);     // A light turqoise style green
        Color DarkerTurqoise = Color.FromArgb(50, 94, 61);      // A darker turqoise-forest green
        Color Nuclear = Color.FromArgb(7, 247, 47);             // A bright nuclear green
        Color DarkNuclear = Color.FromArgb(16, 176, 43);        // A darker nuclear green
        Color DarkFadedGreen = Color.FromArgb(205, 217, 207);   // A dark faded green
        Color WhiteSoft = Color.FromArgb(245, 245, 245);        // A soft white for text and borders

        // Set the background color of the form
        this.BackColor = DarkGreenBG;

        // Set the foreground color of the form (text color)
        this.ForeColor = WhiteSoft;

        // Set the background color of the tab control
        TC_Main.BackColor = DarkFadedGreen;

        // Set the background color of each tab page
        foreach (TabPage page in TC_Main.TabPages)
        {
            page.BackColor = DarkGreenBG;
        }

        // Set the background color of the property grid
        PG_Hub.BackColor = DarkFadedGreen;
        PG_Hub.LineColor = DarkerTurqoise;
        PG_Hub.CategoryForeColor = WhiteSoft;
        PG_Hub.CategorySplitterColor = DarkNuclear;
        PG_Hub.HelpBackColor = DarkGreenBG;
        PG_Hub.HelpForeColor = WhiteSoft;
        PG_Hub.ViewBackColor = DarkGreenBG;
        PG_Hub.ViewForeColor = WhiteSoft;

        // Set the background color of the rich text box
        RTB_Logs.BackColor = DarkGreenBG;
        RTB_Logs.ForeColor = WhiteSoft;

        // Set colors for other controls
        TB_IP.BackColor = DarkerTurqoise;
        TB_IP.ForeColor = WhiteSoft;

        CB_Routine.BackColor = DarkerTurqoise;
        CB_Routine.ForeColor = WhiteSoft;

        NUD_Port.BackColor = DarkerTurqoise;
        NUD_Port.ForeColor = WhiteSoft;

        B_New.BackColor = DarkerTurqoise;
        B_New.ForeColor = WhiteSoft;

        FLP_Bots.BackColor = DarkerTurqoise;

        CB_Protocol.BackColor = DarkGreenBG;
        CB_Protocol.ForeColor = WhiteSoft;

        comboBox1.BackColor = DarkGreenBG;
        comboBox1.ForeColor = WhiteSoft;

        B_Stop.BackColor = LightTurqoise;
        B_Stop.ForeColor = DarkGreenBG;

        B_Start.BackColor = LightTurqoise;
        B_Start.ForeColor = DarkGreenBG;

        B_RebootStop.BackColor = DarkNuclear;
        B_RebootStop.ForeColor = WhiteSoft;

        updater.BackColor = DarkNuclear;
        updater.ForeColor = WhiteSoft;

    }

    private void ApplyBlueTheme()
    {
        // Define Blue Mode theme colors
        Color DarkBlueBG = Color.FromArgb(0, 54, 99);            // A muted dark blue
        Color LightBlue = Color.FromArgb(173, 216, 230);          // A light matte blue
        Color DarkerBlue = Color.FromArgb(21, 63, 97);            // A darker blue
        Color BrightBlue = Color.FromArgb(100, 149, 237);         // A brighter blue for accents
        Color DarkFadedBlue = Color.FromArgb(54, 91, 122);      // A dark faded blue
        Color WhiteSoft = Color.FromArgb(245, 245, 245);          // A soft white for text and borders

        // Set the background color of the form
        this.BackColor = DarkBlueBG;

        // Set the foreground color of the form (text color)
        this.ForeColor = WhiteSoft;

        // Set the background color of the tab control
        TC_Main.BackColor = DarkFadedBlue;

        // Set the background color of each tab page
        foreach (TabPage page in TC_Main.TabPages)
        {
            page.BackColor = DarkBlueBG;
        }

        // Set the background color of the property grid
        PG_Hub.BackColor = DarkFadedBlue;
        PG_Hub.LineColor = BrightBlue;
        PG_Hub.CategoryForeColor = WhiteSoft;
        PG_Hub.CategorySplitterColor = DarkerBlue;
        PG_Hub.HelpBackColor = DarkBlueBG;
        PG_Hub.HelpForeColor = WhiteSoft;
        PG_Hub.ViewBackColor = DarkBlueBG;
        PG_Hub.ViewForeColor = WhiteSoft;

        // Set the background color of the rich text box
        RTB_Logs.BackColor = DarkBlueBG;
        RTB_Logs.ForeColor = WhiteSoft;

        // Set colors for other controls
        TB_IP.BackColor = DarkerBlue;
        TB_IP.ForeColor = WhiteSoft;

        CB_Routine.BackColor = DarkerBlue;
        CB_Routine.ForeColor = WhiteSoft;

        NUD_Port.BackColor = DarkerBlue;
        NUD_Port.ForeColor = WhiteSoft;

        B_New.BackColor = DarkerBlue;
        B_New.ForeColor = WhiteSoft;

        FLP_Bots.BackColor = DarkerBlue;

        CB_Protocol.BackColor = DarkBlueBG;
        CB_Protocol.ForeColor = WhiteSoft;

        comboBox1.BackColor = DarkBlueBG;
        comboBox1.ForeColor = WhiteSoft;

        B_Stop.BackColor = LightBlue;
        B_Stop.ForeColor = DarkBlueBG;

        B_Start.BackColor = LightBlue;
        B_Start.ForeColor = DarkBlueBG;

        B_RebootStop.BackColor = BrightBlue;
        B_RebootStop.ForeColor = WhiteSoft;

        updater.BackColor = BrightBlue;
        updater.ForeColor = WhiteSoft;

    }

    private void ApplyAkatsukiTheme()
    {
        // Define Akatsuki theme colors
        Color DarkNearBlack = Color.FromArgb(10, 10, 10);           // A dark, near-black background
        Color CrimsonBloodyRed = Color.FromArgb(187, 0, 0);         // A crimson bloody red color
        Color BrightRed = Color.FromArgb(255, 87, 51);              // Bright red for accents
        Color SoftGrey = Color.FromArgb(211, 211, 211);             // Soft grey for text and borders
        Color White = Color.FromArgb(255, 255, 255);                // White for highlights and outlines

        // Set the background color of the form
        this.BackColor = DarkNearBlack;

        // Set the foreground color of the form (text color)
        this.ForeColor = SoftGrey;

        // Set the background color of the tab control
        TC_Main.BackColor = DarkNearBlack;

        // Set the background color of each tab page
        foreach (TabPage page in TC_Main.TabPages)
        {
            page.BackColor = DarkNearBlack;
        }

        // Set the background color of the property grid
        PG_Hub.BackColor = DarkNearBlack;
        PG_Hub.LineColor = DarkNearBlack;
        PG_Hub.CategoryForeColor = SoftGrey;
        PG_Hub.CategorySplitterColor = DarkNearBlack;
        PG_Hub.HelpBackColor = DarkNearBlack;
        PG_Hub.HelpForeColor = SoftGrey;
        PG_Hub.ViewBackColor = DarkNearBlack;
        PG_Hub.ViewForeColor = SoftGrey;

        // Set the background color of the rich text box
        RTB_Logs.BackColor = DarkNearBlack;
        RTB_Logs.ForeColor = SoftGrey;

        // Set colors for other controls
        TB_IP.BackColor = DarkNearBlack;  // IP Background color
        TB_IP.ForeColor = BrightRed;      // Text color for IP box

        CB_Routine.BackColor = DarkNearBlack;
        CB_Routine.ForeColor = SoftGrey;

        NUD_Port.BackColor = DarkNearBlack;  // Port Background color
        NUD_Port.ForeColor = BrightRed;      // Text color for Port box

        B_New.BackColor = DarkNearBlack;
        B_New.ForeColor = BrightRed;

        FLP_Bots.BackColor = DarkNearBlack;

        CB_Protocol.BackColor = CrimsonBloodyRed;
        CB_Protocol.ForeColor = SoftGrey;

        comboBox1.BackColor = CrimsonBloodyRed;
        comboBox1.ForeColor = SoftGrey;

        B_Stop.BackColor = DarkNearBlack;
        B_Stop.ForeColor = BrightRed;

        B_Start.BackColor = DarkNearBlack;
        B_Start.ForeColor = BrightRed;

        B_RebootStop.BackColor = DarkNearBlack;
        B_RebootStop.ForeColor = BrightRed;

        updater.BackColor = DarkNearBlack;
        updater.ForeColor = BrightRed;
    }

    private void ApplyNarutoTheme()
    {
        // Define Naruto Shippuden theme colors
        Color BlackGray = Color.FromArgb(40, 40, 40);         // A black-gray background
        Color MatteOrange = Color.FromArgb(255, 128, 0);      // A matte orange color
        Color SoftGray = Color.FromArgb(169, 169, 169);       // Soft gray for text and borders
        Color White = Color.FromArgb(255, 255, 255);          // White for highlights and outlines

        // Set the background color of the form
        this.BackColor = BlackGray;

        // Set the foreground color of the form (text color)
        this.ForeColor = SoftGray;

        // Set the background color of the tab control
        TC_Main.BackColor = BlackGray;

        // Set the background color of each tab page
        foreach (TabPage page in TC_Main.TabPages)
        {
            page.BackColor = BlackGray;
        }

        // Set the background color of the property grid
        PG_Hub.BackColor = BlackGray;
        PG_Hub.LineColor = SoftGray;
        PG_Hub.CategoryForeColor = BlackGray;
        PG_Hub.CategorySplitterColor = SoftGray;
        PG_Hub.HelpBackColor = BlackGray;
        PG_Hub.HelpForeColor = SoftGray;
        PG_Hub.ViewBackColor = BlackGray;
        PG_Hub.ViewForeColor = SoftGray;

        // Set the background color of the rich text box
        RTB_Logs.BackColor = BlackGray;
        RTB_Logs.ForeColor = SoftGray;

        // Set colors for other controls
        TB_IP.BackColor = BlackGray;       // IP Background color
        TB_IP.ForeColor = MatteOrange;     // Text color for IP box

        CB_Routine.BackColor = BlackGray;
        CB_Routine.ForeColor = SoftGray;

        NUD_Port.BackColor = BlackGray;    // Port Background color
        NUD_Port.ForeColor = MatteOrange;   // Text color for Port box

        B_New.BackColor = BlackGray;
        B_New.ForeColor = MatteOrange;

        FLP_Bots.BackColor = BlackGray;

        CB_Protocol.BackColor = MatteOrange;
        CB_Protocol.ForeColor = SoftGray;

        comboBox1.BackColor = MatteOrange;
        comboBox1.ForeColor = SoftGray;

        B_Stop.BackColor = BlackGray;
        B_Stop.ForeColor = MatteOrange;

        B_Start.BackColor = BlackGray;
        B_Start.ForeColor = MatteOrange;

        B_RebootStop.BackColor = BlackGray;
        B_RebootStop.ForeColor = MatteOrange;

        updater.BackColor = BlackGray;
        updater.ForeColor = MatteOrange;
    }
        private void ApplyShinyMewtwoTheme()
    {
        // Define Shiny Mewtwo theme colors
        Color SoftWhite = Color.FromArgb(230, 230, 230);        // A darker shade of white, closer to gray
        Color SoftLimeGreen = Color.FromArgb(175, 215, 95);     // A soft matte-like lime green color
        Color Black = Color.Black;                              // Black color

        // Set the background color of the form
        this.BackColor = SoftWhite;

        // Set the foreground color of the form (text color)
        this.ForeColor = Black;  // Change SoftWhite to Black

        // Set the background color of the tab control
        TC_Main.BackColor = SoftWhite;

        // Set the background color of each tab page
        foreach (TabPage page in TC_Main.TabPages)
        {
            page.BackColor = SoftWhite;
        }

        // Set the background color of the property grid
        PG_Hub.BackColor = SoftWhite;
        PG_Hub.LineColor = SoftLimeGreen;
        PG_Hub.CategoryForeColor = Black;  // Change SoftWhite to Black
        PG_Hub.CategorySplitterColor = SoftLimeGreen;
        PG_Hub.HelpBackColor = SoftWhite;
        PG_Hub.HelpForeColor = Black;  // Change SoftWhite to Black
        PG_Hub.ViewBackColor = SoftWhite;
        PG_Hub.ViewForeColor = Black;  // Change SoftWhite to Black

        // Set the background color of the rich text box
        RTB_Logs.BackColor = SoftWhite;
        RTB_Logs.ForeColor = Black;  // Change SoftWhite to Black

        // Set colors for other controls
        TB_IP.BackColor = SoftLimeGreen;       // Change SoftWhite to SoftLimeGreen for IP box
        TB_IP.ForeColor = Black;                // Change SoftWhite to Black for IP box

        CB_Routine.BackColor = SoftWhite;
        CB_Routine.ForeColor = Black;  // Change SoftWhite to Black

        NUD_Port.BackColor = SoftLimeGreen;    // Change SoftWhite to SoftLimeGreen for Port box
        NUD_Port.ForeColor = Black;             // Change SoftWhite to Black for Port box

        B_New.BackColor = SoftLimeGreen;
        B_New.ForeColor = Black;   // Change SoftWhite to Black

        FLP_Bots.BackColor = SoftWhite;

        CB_Protocol.BackColor = SoftLimeGreen;
        CB_Protocol.ForeColor = Black;  // Change SoftWhite to Black

        comboBox1.BackColor = SoftLimeGreen;
        comboBox1.ForeColor = Black;  // Change SoftWhite to Black

        B_Stop.BackColor = SoftLimeGreen;
        B_Stop.ForeColor = Black;

        B_Start.BackColor = SoftLimeGreen;
        B_Start.ForeColor = Black;

        B_RebootStop.BackColor = SoftLimeGreen;
        B_RebootStop.ForeColor = Black;

        updater.BackColor = SoftLimeGreen;
        updater.ForeColor = Black;
    }

    private void ApplyShinyUmbreonTheme()
    {
        // Define Shiny Umbreon theme colors
        Color DarkBlue = Color.FromArgb(0, 102, 204);        // A brighter, more neon blue color
        Color Black = Color.Black;                           // Black color
        Color White = Color.White;                           // White color
        Color DarkGray = Color.FromArgb(64, 64, 64);         // A dark gray color

        // Set the background color of the form
        this.BackColor = DarkGray;  // Switch to DarkGray

        // Set the foreground color of the form (text color)
        this.ForeColor = White;  // Change to White

        // Set the background color of the tab control
        TC_Main.BackColor = DarkGray;  // Switch to DarkGray

        // Set the background color of each tab page
        foreach (TabPage page in TC_Main.TabPages)
        {
            page.BackColor = DarkGray;  // Switch to DarkGray
        }

        // Set the background color of the property grid
        PG_Hub.BackColor = Black;  // Switch to DarkGray
        PG_Hub.LineColor = DarkGray;   // Switch to Black
        PG_Hub.CategoryForeColor = White;   // Switch to Black
        PG_Hub.CategorySplitterColor = DarkBlue;   // Switch to DarkBlue
        PG_Hub.HelpBackColor = DarkGray;  // Switch to DarkGray
        PG_Hub.HelpForeColor = Black;  // Switch to Black
        PG_Hub.ViewBackColor = DarkGray;  // Switch to DarkGray
        PG_Hub.ViewForeColor = White;  // Switch to White

        // Set the background color of the rich text box
        RTB_Logs.BackColor = DarkGray;  // Switch to DarkGray
        RTB_Logs.ForeColor = DarkBlue;  // Switch to DarkBlue

        // Set colors for other controls
        TB_IP.BackColor = Black;        // Switch to Black
        TB_IP.ForeColor = White;        // Switch to White

        CB_Routine.BackColor = Black;    // Switch to Black
        CB_Routine.ForeColor = White;    // Switch to White

        NUD_Port.BackColor = Black;      // Switch to Black
        NUD_Port.ForeColor = White;      // Switch to White

        B_New.BackColor = DarkBlue;      // Switch to DarkBlue
        B_New.ForeColor = Black;         // Switch to Black

        FLP_Bots.BackColor = DarkGray;

        CB_Protocol.BackColor = DarkBlue;
        CB_Protocol.ForeColor = DarkBlue;

        comboBox1.BackColor = DarkBlue;
        comboBox1.ForeColor = DarkBlue;

        B_Stop.BackColor = DarkBlue;
        B_Stop.ForeColor = White;

        B_Start.BackColor = DarkBlue;
        B_Start.ForeColor = White;

        B_RebootStop.BackColor = DarkBlue;
        B_RebootStop.ForeColor = White;

        updater.BackColor = DarkBlue;
        updater.ForeColor = White;
    }
    private void ApplyBlackAndWhiteTheme()
    {
        // Define Black & White theme colors
        Color SoftGray = Color.FromArgb(180, 180, 180);      // Soft gray color
        Color MediumGray = Color.FromArgb(120, 120, 120);    // Medium gray color
        Color White = Color.White;
        Color Black = Color.Black;  // White color for fonts

        // Set the background color of the Hub form
        this.BackColor = SoftGray;                           // Soft gray for the background

        // Set the foreground color of the main status form
        this.ForeColor = Black;                              // Black text color

        // Set the background color of the tab control
        TC_Main.BackColor = MediumGray;                      // Medium gray for tab control

        // Set the background color of each tab page
        foreach (TabPage page in TC_Main.TabPages)
        {
            page.BackColor = SoftGray;                       // Soft gray for each tab page
        }

        // Set the background color of the Hub
        PG_Hub.BackColor = SoftGray;                        // Soft gray for Hub
        PG_Hub.LineColor = SoftGray;                        // Line color for Hub
        PG_Hub.CategoryForeColor = Black;                   // Black font color for category text
        PG_Hub.CategorySplitterColor = SoftGray;            // Category splitter color
        PG_Hub.HelpBackColor = SoftGray;                   // Help background color
        PG_Hub.HelpForeColor = Black;                       // Black help text color
        PG_Hub.ViewBackColor = SoftGray;
        PG_Hub.ViewForeColor = Black;

        // Set the colors of the Log tab
        RTB_Logs.BackColor = SoftGray;                     // Soft gray for log background
        RTB_Logs.ForeColor = Black;                         // Black text color for logs

        // Set the colors of the IP form
        TB_IP.BackColor = MediumGray;                       // Medium gray for IP form box background
        TB_IP.ForeColor = White;                             // White text color for IP form box

        CB_Routine.BackColor = SoftGray;                   // Soft gray for combo box background
        CB_Routine.ForeColor = Black;                        // Black text color for combo box

        NUD_Port.BackColor = MediumGray;                    // Medium gray for Port box background
        NUD_Port.ForeColor = White;                          // White text color for Port box

        B_New.BackColor = MediumGray;                       // Medium gray for button background
        B_New.ForeColor = Black;                             // Black text color for button

        FLP_Bots.BackColor = SoftGray;                      // Soft gray for panel behind trade type and status information

        CB_Protocol.BackColor = SoftGray;                  // Soft gray for protocol combo box background
        CB_Protocol.ForeColor = Black;                       // Black text color for protocol combo box

        comboBox1.BackColor = MediumGray;                   // Medium gray for combo box background
        comboBox1.ForeColor = Black;                         // Black text color for combo box

        B_Stop.BackColor = MediumGray;                      // Medium gray for STOP button background
        B_Stop.ForeColor = White;                            // White text color for STOP button font

        B_Start.BackColor = MediumGray;                     // Medium gray for START button background
        B_Start.ForeColor = White;                           // White text color for START button font

        B_RebootStop.BackColor = MediumGray;                // Medium gray for REBOOT STOP button background
        B_RebootStop.ForeColor = White;                      // White text color for REBOOT STOP button font

        updater.BackColor = MediumGray;                     // Medium gray for updater background
        updater.ForeColor = White;                           // White text color for updater font
    }
    private void ApplyPokemonScarletTheme()
    {
        // Define Pokémon Scarlet theme colors
        Color SoftRed = Color.FromArgb(204, 102, 102);       // Softened matte red color
        Color DarkRed = Color.FromArgb(102, 0, 0);           // Softened dark red color
        Color MatteYellow = Color.FromArgb(242, 195, 0);     // Softened matte yellow color
        Color White = Color.FromArgb(240, 240, 240);         // Softened white color

        // Set the background color of the Hub form
        this.BackColor = SoftRed;                            // Softened red for the background

        // Set the foreground color of the main status form
        this.ForeColor = White;                             // White text color

        // Set the background color of the tab control
        TC_Main.BackColor = SoftRed;                         // Soft red for tab control background

        // Set the background color of each tab page
        foreach (TabPage page in TC_Main.TabPages)
        {
            page.BackColor = MatteYellow;                    // Matte yellow for each tab page
        }

        // Set the background color of the Hub
        PG_Hub.BackColor = SoftRed;                          // Softened red for Hub background
        PG_Hub.LineColor = DarkRed;                          // Dark red for line color in Hub
        PG_Hub.CategoryForeColor = White;                    // White font color for category text
        PG_Hub.CategorySplitterColor = SoftRed;              // Softened red for category splitter color
        PG_Hub.HelpBackColor = SoftRed;                     // Softened red for help background color
        PG_Hub.HelpForeColor = White;                        // White font color for help text
        PG_Hub.ViewBackColor = SoftRed;
        PG_Hub.ViewForeColor = White;

        // Set the colors of the Log tab
        RTB_Logs.BackColor = SoftRed;                       // Softened red for log background
        RTB_Logs.ForeColor = White;                          // White text color for logs

        // Set the colors of the IP form
        TB_IP.BackColor = SoftRed;                      // Dark red for IP form box background
        TB_IP.ForeColor = White;                             // White text color for IP form box

        CB_Routine.BackColor = SoftRed;                      // Softened red for combo box background
        CB_Routine.ForeColor = White;                         // White text color for combo box

        NUD_Port.BackColor = SoftRed;                    // Dark red for Port box background
        NUD_Port.ForeColor = White;                          // White text color for Port box

        B_New.BackColor = DarkRed;                       // Dark red for button background
        B_New.ForeColor = White;                             // White text color for button

        FLP_Bots.BackColor = DarkRed;                        // Dark red for panel behind trade type and status information

        CB_Protocol.BackColor = SoftRed;                     // Softened red for protocol combo box background
        CB_Protocol.ForeColor = White;                        // White text color for protocol combo box

        comboBox1.BackColor = MatteYellow;                   // Matte yellow for combo box background
        comboBox1.ForeColor = White;                         // White text color for combo box

        B_Stop.BackColor = DarkRed;                      // Dark red for STOP button background
        B_Stop.ForeColor = White;                            // White text color for STOP button font

        B_Start.BackColor = DarkRed;                     // Dark red for START button background
        B_Start.ForeColor = White;                           // White text color for START button font

        B_RebootStop.BackColor = DarkRed;                // Dark red for REBOOT STOP button background
        B_RebootStop.ForeColor = White;                      // White text color for REBOOT STOP button font

        updater.BackColor = DarkRed;                     // Matte yellow for updater background
        updater.ForeColor = White;                           // White text color for updater font
    }
    private void ApplyPokemonVioletTheme()
    {
        // Define Pokémon Scarlet theme colors
        Color SoftViolet = Color.FromArgb(153, 102, 204);       // Softened matte violet color
        Color DarkViolet = Color.FromArgb(102, 0, 102);         // Softened dark violet color
        Color MatteYellow = Color.FromArgb(242, 195, 0);        // Softened matte yellow color
        Color White = Color.FromArgb(240, 240, 240);            // Softened white color

        // Set the background color of the Hub form
        this.BackColor = DarkViolet;                            // Softened violet for the background

        // Set the foreground color of the main status form
        this.ForeColor = White;                                 // White text color

        // Set the background color of the tab control
        TC_Main.BackColor = DarkViolet;                         // Soft violet for tab control background

        // Set the background color of each tab page
        foreach (TabPage page in TC_Main.TabPages)
        {
            page.BackColor = MatteYellow;                        // Matte yellow for each tab page
        }

        // Set the background color of the Hub
        PG_Hub.BackColor = DarkViolet;                          // Softened violet for Hub background
        PG_Hub.LineColor = DarkViolet;                          // Dark violet for line color in Hub
        PG_Hub.CategoryForeColor = White;                       // White font color for category text
        PG_Hub.CategorySplitterColor = SoftViolet;              // Softened violet for category splitter color
        PG_Hub.HelpBackColor = SoftViolet;                     // Softened violet for help background color
        PG_Hub.HelpForeColor = White;                           // White font color for help text
        PG_Hub.ViewBackColor = SoftViolet;
        PG_Hub.ViewForeColor = White;

        // Set the colors of the Log tab
        RTB_Logs.BackColor = SoftViolet;                        // Softened violet for log background
        RTB_Logs.ForeColor = White;                              // White text color for logs

        // Set the colors of the IP form
        TB_IP.BackColor = DarkViolet;                           // Soft violet for IP form box background
        TB_IP.ForeColor = White;                                 // White text color for IP form box

        CB_Routine.BackColor = SoftViolet;                       // Softened violet for combo box background
        CB_Routine.ForeColor = White;                            // White text color for combo box

        NUD_Port.BackColor = DarkViolet;                         // Soft violet for Port box background
        NUD_Port.ForeColor = White;                              // White text color for Port box

        B_New.BackColor = DarkViolet;                            // Dark violet for button background
        B_New.ForeColor = White;                                 // White text color for button

        FLP_Bots.BackColor = DarkViolet;                         // Dark violet for panel behind trade type and status information

        CB_Protocol.BackColor = SoftViolet;                      // Softened violet for protocol combo box background
        CB_Protocol.ForeColor = White;                           // White text color for protocol combo box

        comboBox1.BackColor = MatteYellow;                      // Matte yellow for combo box background
        comboBox1.ForeColor = White;                            // White text color for combo box

        B_Stop.BackColor = DarkViolet;                          // Dark violet for STOP button background
        B_Stop.ForeColor = White;                                // White text color for STOP button font

        B_Start.BackColor = DarkViolet;                         // Dark violet for START button background
        B_Start.ForeColor = White;                               // White text color for START button font

        B_RebootStop.BackColor = DarkViolet;                    // Dark violet for REBOOT STOP button background
        B_RebootStop.ForeColor = White;                          // White text color for REBOOT STOP button font

        updater.BackColor = DarkViolet;                         // Dark violet for updater background
        updater.ForeColor = White;                               // White text color for updater font
    }
    private void ApplyRainbowTheme()
    {
        // Define Rainbow theme colors
        Color DarkRed = Color.FromArgb(102, 0, 0);          // Dark matte red color
        Color DarkOrange = Color.FromArgb(204, 102, 0);     // Dark matte orange color
        Color DarkYellow = Color.FromArgb(153, 153, 0);     // Dark matte yellow color
        Color DarkGreen = Color.FromArgb(0, 102, 0);        // Dark matte green color
        Color DarkBlue = Color.FromArgb(0, 51, 102);        // Dark matte blue color
        Color DarkIndigo = Color.FromArgb(51, 0, 102);      // Dark matte indigo color
        Color DarkViolet = Color.FromArgb(102, 0, 102);     // Dark matte violet color
        Color White = Color.FromArgb(240, 240, 240);        // Softened white color

        // Set the background color of the Hub form
        this.BackColor = DarkRed;                            // Dark red for the background

        // Set the foreground color of the main status form
        this.ForeColor = White;                              // White text color

        // Set the background color of the tab control
        TC_Main.BackColor = DarkOrange;                      // Dark orange for tab control background

        // Set the background color of each tab page
        foreach (TabPage page in TC_Main.TabPages)
        {
            page.BackColor = DarkYellow;                     // Dark yellow for each tab page
        }

        // Set the background color of the Hub
        PG_Hub.BackColor = DarkGreen;                       // Dark green for Hub background
        PG_Hub.LineColor = DarkBlue;                        // Dark blue for line color in Hub
        PG_Hub.CategoryForeColor = White;                   // White font color for category text
        PG_Hub.CategorySplitterColor = DarkGreen;           // Dark green for category splitter color
        PG_Hub.HelpBackColor = DarkIndigo;                 // Dark indigo for help background color
        PG_Hub.HelpForeColor = White;                       // White font color for help text
        PG_Hub.ViewBackColor = DarkViolet;
        PG_Hub.ViewForeColor = White;

        // Set the colors of the Log tab
        RTB_Logs.BackColor = DarkRed;                       // Dark red for log background
        RTB_Logs.ForeColor = White;                         // White text color for logs

        // Set the colors of the IP form
        TB_IP.BackColor = DarkOrange;                       // Dark orange for IP form box background
        TB_IP.ForeColor = White;                             // White text color for IP form box

        CB_Routine.BackColor = DarkYellow;                  // Dark yellow for combo box background
        CB_Routine.ForeColor = White;                        // White text color for combo box

        NUD_Port.BackColor = DarkGreen;                     // Dark green for Port box background
        NUD_Port.ForeColor = White;                          // White text color for Port box

        B_New.BackColor = DarkBlue;                         // Dark blue for button background
        B_New.ForeColor = White;                             // White text color for button

        FLP_Bots.BackColor = DarkIndigo;                    // Dark indigo for panel behind trade type and status information

        CB_Protocol.BackColor = DarkViolet;                 // Dark violet for protocol combo box background
        CB_Protocol.ForeColor = White;                       // White text color for protocol combo box

        comboBox1.BackColor = DarkRed;                      // Dark red for combo box background
        comboBox1.ForeColor = White;                         // White text color for combo box

        B_Stop.BackColor = DarkOrange;                      // Dark orange for STOP button background
        B_Stop.ForeColor = White;                            // White text color for STOP button font

        B_Start.BackColor = DarkYellow;                     // Dark yellow for START button background
        B_Start.ForeColor = White;                           // White text color for START button font

        B_RebootStop.BackColor = DarkGreen;                 // Dark green for REBOOT STOP button background
        B_RebootStop.ForeColor = White;                      // White text color for REBOOT STOP button font

        updater.BackColor = DarkBlue;                      // Dark blue for updater background
        updater.ForeColor = White;                           // White text color for updater font
    }
    private void ApplyPitchBlackTheme()
    {
        // Set the background color of the Hub form
        this.BackColor = Color.Black;               // Black background

        // Set the foreground color of the main status form
        this.ForeColor = Color.White;               // White text color

        // Set the background color of the tab control
        TC_Main.BackColor = Color.Black;            // Black background for tab control

        // Set the background color of each tab page
        foreach (TabPage page in TC_Main.TabPages)
        {
            page.BackColor = Color.Black;           // Black background for each tab page
        }

        // Set the background color of the Hub
        PG_Hub.BackColor = Color.Black;            // Black background for Hub
        PG_Hub.LineColor = Color.White;            // White line color in Hub
        PG_Hub.CategoryForeColor = Color.Black;    // White font color for category text
        PG_Hub.CategorySplitterColor = Color.Black; // Black category splitter color
        PG_Hub.HelpBackColor = Color.Black;        // Black help background color
        PG_Hub.HelpForeColor = Color.White;        // White help text color
        PG_Hub.ViewBackColor = Color.Black;        // Black view background color
        PG_Hub.ViewForeColor = Color.White;        // White view text color

        // Set the colors of the Log tab
        RTB_Logs.BackColor = Color.Black;          // Black log background
        RTB_Logs.ForeColor = Color.White;          // White log text color

        // Set the colors of the IP form
        TB_IP.BackColor = Color.Black;             // Black IP form box background
        TB_IP.ForeColor = Color.White;             // White IP form box text color

        CB_Routine.BackColor = Color.Black;        // Black combo box background
        CB_Routine.ForeColor = Color.White;        // White combo box text color

        NUD_Port.BackColor = Color.Black;          // Black Port box background
        NUD_Port.ForeColor = Color.White;          // White Port box text color

        B_New.BackColor = Color.Black;             // Black button background
        B_New.ForeColor = Color.White;             // White button text color

        FLP_Bots.BackColor = Color.Black;          // Black panel background behind trade type and status information

        CB_Protocol.BackColor = Color.Black;       // Black protocol combo box background
        CB_Protocol.ForeColor = Color.White;       // White protocol combo box text color

        comboBox1.BackColor = Color.Black;         // Black combo box background
        comboBox1.ForeColor = Color.White;         // White combo box text color

        B_Stop.BackColor = Color.Black;            // Black STOP button background
        B_Stop.ForeColor = Color.White;            // White STOP button text color

        B_Start.BackColor = Color.Black;           // Black START button background
        B_Start.ForeColor = Color.White;           // White START button text color

        B_RebootStop.BackColor = Color.Black;      // Black REBOOT STOP button background
        B_RebootStop.ForeColor = Color.White;      // White REBOOT STOP button text color

        updater.BackColor = Color.Black;           // Black updater background
        updater.ForeColor = Color.White;           // White updater text color
    }
}


