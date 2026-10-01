using System.Text.Json;

namespace NJS.InventorAddIn;

internal sealed class AddInSettings
{
    public const string KeepCurrentAppearanceOption = "(Keep current appearance)";

    private static readonly string SettingsFilePath = System.IO.Path.Combine(
        System.Environment.GetFolderPath(System.Environment.SpecialFolder.LocalApplicationData),
        "NJSTools",
        "InventorAddIn",
        "settings.json");

    public static string DefaultBomTemplatePath => System.IO.Path.Combine(
        System.Environment.GetFolderPath(System.Environment.SpecialFolder.MyDocuments),
        "Bill of Materials",
        "BoM Template.xlsx");

    public static string DefaultBomOutputDirectory => System.IO.Path.Combine(
        System.Environment.GetFolderPath(System.Environment.SpecialFolder.MyDocuments),
        "Bill of Materials",
        "Draft");

    public static string DefaultDigitalImageOutputDirectory =>
        System.Environment.GetFolderPath(System.Environment.SpecialFolder.MyPictures);

    public static string DefaultDigitalImageMaterial => string.Empty;

    public string BomTemplatePath { get; set; } = string.Empty;
    public string BomOutputDirectory { get; set; } = DefaultBomOutputDirectory;
    public string DigitalImageOutputDirectory { get; set; } = DefaultDigitalImageOutputDirectory;
    public string DigitalImageMaterial { get; set; } = DefaultDigitalImageMaterial;

    public static AddInSettings Load()
    {
        try
        {
            if (System.IO.File.Exists(SettingsFilePath))
            {
                AddInSettings? settings = JsonSerializer.Deserialize<AddInSettings>(
                    System.IO.File.ReadAllText(SettingsFilePath));
                if (settings is not null)
                {
                    settings.BomTemplatePath ??= string.Empty;
                    settings.BomOutputDirectory ??= DefaultBomOutputDirectory;
                    settings.DigitalImageOutputDirectory ??= DefaultDigitalImageOutputDirectory;
                    settings.DigitalImageMaterial ??= DefaultDigitalImageMaterial;
                    return settings;
                }
            }
        }
        catch
        {
        }

        return new AddInSettings();
    }

    public void Save()
    {
        string directory = System.IO.Path.GetDirectoryName(SettingsFilePath)!;
        System.IO.Directory.CreateDirectory(directory);
        System.IO.File.WriteAllText(
            SettingsFilePath,
            JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true }));
    }
}

internal sealed class AddInSettingsDialog : System.Windows.Forms.Form
{
    private sealed record ImageMaterialOption(string Name, bool IsAvailable)
    {
        public override string ToString() => IsAvailable ? Name : $"{Name} (not available)";
    }

    private readonly System.Windows.Forms.TextBox _templatePathBox = new();
    private readonly System.Windows.Forms.TextBox _outputDirectoryBox = new();
    private readonly System.Windows.Forms.TextBox _digitalImageOutputDirectoryBox = new();
    private readonly System.Windows.Forms.ComboBox _digitalImageMaterialBox = new();

    public AddInSettings Settings { get; private set; }

    public AddInSettingsDialog(AddInSettings settings, IReadOnlyList<string> digitalImageMaterials)
    {
        Settings = settings;
        Text = "NJS Tools | Add-On Settings";
        FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
        StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
        MinimizeBox = false;
        MaximizeBox = false;
        ShowInTaskbar = false;
        ClientSize = new System.Drawing.Size(620, 340);
        Font = new System.Drawing.Font("Segoe UI", 9F);

        var layout = new System.Windows.Forms.TableLayoutPanel
        {
            Dock = System.Windows.Forms.DockStyle.Fill,
            Padding = new System.Windows.Forms.Padding(16),
            ColumnCount = 1,
            RowCount = 3
        };
        layout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 50));
        layout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 50));
        layout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 44));

        var group = new System.Windows.Forms.GroupBox
        {
            Text = "Bill of Materials",
            Dock = System.Windows.Forms.DockStyle.Fill,
            Padding = new System.Windows.Forms.Padding(10)
        };
        var fields = new System.Windows.Forms.TableLayoutPanel
        {
            Dock = System.Windows.Forms.DockStyle.Fill,
            ColumnCount = 4,
            RowCount = 2,
            Padding = new System.Windows.Forms.Padding(4)
        };
        fields.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 120));
        fields.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100));
        fields.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 82));
        fields.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 82));
        fields.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 50));
        fields.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 50));

        _templatePathBox.Text = settings.BomTemplatePath;
        _outputDirectoryBox.Text = settings.BomOutputDirectory;
        _digitalImageOutputDirectoryBox.Text = settings.DigitalImageOutputDirectory;
        _digitalImageMaterialBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;

        var keepCurrentOption = new ImageMaterialOption(AddInSettings.KeepCurrentAppearanceOption, true);
        _digitalImageMaterialBox.Items.Add(keepCurrentOption);
        foreach (string materialName in digitalImageMaterials)
        {
            _digitalImageMaterialBox.Items.Add(new ImageMaterialOption(materialName, true));
        }

        ImageMaterialOption? selectedMaterial;
        if (string.IsNullOrWhiteSpace(settings.DigitalImageMaterial) ||
            string.Equals(settings.DigitalImageMaterial, AddInSettings.KeepCurrentAppearanceOption, StringComparison.OrdinalIgnoreCase))
        {
            selectedMaterial = keepCurrentOption;
        }
        else
        {
            selectedMaterial = _digitalImageMaterialBox.Items
                .Cast<ImageMaterialOption>()
                .FirstOrDefault(option => string.Equals(
                    option.Name,
                    settings.DigitalImageMaterial,
                    StringComparison.OrdinalIgnoreCase));
            if (selectedMaterial is null)
            {
                selectedMaterial = new ImageMaterialOption(settings.DigitalImageMaterial, false);
                _digitalImageMaterialBox.Items.Insert(1, selectedMaterial);
            }
        }

        _digitalImageMaterialBox.SelectedItem = selectedMaterial;

        AddFieldRow(fields, 0, "Template workbook", _templatePathBox, BrowseTemplate, "Default", (_, _) => _templatePathBox.Clear());
        AddFieldRow(fields, 1, "Draft output folder", _outputDirectoryBox, BrowseOutputFolder, "Default", (_, _) => _outputDirectoryBox.Text = AddInSettings.DefaultBomOutputDirectory);
        group.Controls.Add(fields);
        layout.Controls.Add(group, 0, 0);

        var digitalImagesGroup = new System.Windows.Forms.GroupBox
        {
            Text = "Digital Images",
            Dock = System.Windows.Forms.DockStyle.Fill,
            Padding = new System.Windows.Forms.Padding(10)
        };
        var digitalImagesFields = new System.Windows.Forms.TableLayoutPanel
        {
            Dock = System.Windows.Forms.DockStyle.Fill,
            ColumnCount = 4,
            RowCount = 2,
            Padding = new System.Windows.Forms.Padding(4)
        };
        digitalImagesFields.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 120));
        digitalImagesFields.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100));
        digitalImagesFields.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 82));
        digitalImagesFields.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 82));
        digitalImagesFields.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 50));
        digitalImagesFields.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 50));
        AddFieldRow(
            digitalImagesFields,
            0,
            "Save folder",
            _digitalImageOutputDirectoryBox,
            BrowseDigitalImageOutputFolder,
            "Default",
            (_, _) => _digitalImageOutputDirectoryBox.Text = AddInSettings.DefaultDigitalImageOutputDirectory);
        var imageMaterialLabel = new System.Windows.Forms.Label
        {
            Text = "Image material",
            Dock = System.Windows.Forms.DockStyle.Fill,
            TextAlign = System.Drawing.ContentAlignment.MiddleLeft
        };
        _digitalImageMaterialBox.Dock = System.Windows.Forms.DockStyle.Fill;
        _digitalImageMaterialBox.Margin = new System.Windows.Forms.Padding(3, 5, 6, 5);
        var defaultImageMaterialButton = new System.Windows.Forms.Button
        {
            Text = "Default",
            Dock = System.Windows.Forms.DockStyle.Fill,
            Margin = new System.Windows.Forms.Padding(3, 5, 3, 5)
        };
        defaultImageMaterialButton.Click += (_, _) =>
        {
            _digitalImageMaterialBox.SelectedItem = keepCurrentOption;
        };
        digitalImagesFields.Controls.Add(imageMaterialLabel, 0, 1);
        digitalImagesFields.Controls.Add(_digitalImageMaterialBox, 1, 1);
        digitalImagesFields.SetColumnSpan(_digitalImageMaterialBox, 2);
        digitalImagesFields.Controls.Add(defaultImageMaterialButton, 3, 1);
        digitalImagesGroup.Controls.Add(digitalImagesFields);
        layout.Controls.Add(digitalImagesGroup, 0, 1);

        var footer = new System.Windows.Forms.TableLayoutPanel
        {
            Dock = System.Windows.Forms.DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1,
            Padding = new System.Windows.Forms.Padding(0, 8, 0, 0)
        };
        footer.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100));
        footer.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 160));

        var websiteLink = new System.Windows.Forms.LinkLabel
        {
            Text = "njs.dev",
            AutoSize = true,
            Anchor = System.Windows.Forms.AnchorStyles.Left,
            Margin = new System.Windows.Forms.Padding(3, 0, 0, 0),
            TabStop = true
        };
        websiteLink.LinkClicked += (_, _) =>
        {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = "https://njs.dev",
                UseShellExecute = true
            });
        };

        var buttons = new System.Windows.Forms.FlowLayoutPanel
        {
            Dock = System.Windows.Forms.DockStyle.Fill,
            FlowDirection = System.Windows.Forms.FlowDirection.RightToLeft,
            WrapContents = false,
            Padding = new System.Windows.Forms.Padding(0, 8, 0, 0)
        };
        var saveButton = new System.Windows.Forms.Button { Text = "Save", AutoSize = true };
        var cancelButton = new System.Windows.Forms.Button
        {
            Text = "Cancel",
            AutoSize = true,
            DialogResult = System.Windows.Forms.DialogResult.Cancel
        };
        saveButton.Click += (_, _) => SaveSettings();
        buttons.Controls.Add(saveButton);
        buttons.Controls.Add(cancelButton);
        footer.Controls.Add(websiteLink, 0, 0);
        footer.Controls.Add(buttons, 1, 0);
        layout.Controls.Add(footer, 0, 2);

        Controls.Add(layout);
        AcceptButton = saveButton;
        CancelButton = cancelButton;
    }

    private static void AddFieldRow(
        System.Windows.Forms.TableLayoutPanel layout,
        int row,
        string labelText,
        System.Windows.Forms.TextBox textBox,
        System.EventHandler browseHandler,
        string resetText,
        System.EventHandler resetHandler)
    {
        var label = new System.Windows.Forms.Label
        {
            Text = labelText,
            Dock = System.Windows.Forms.DockStyle.Fill,
            TextAlign = System.Drawing.ContentAlignment.MiddleLeft
        };
        textBox.Dock = System.Windows.Forms.DockStyle.Fill;
        textBox.Margin = new System.Windows.Forms.Padding(3, 5, 6, 5);
        var browseButton = new System.Windows.Forms.Button
        {
            Text = "Browse...",
            Dock = System.Windows.Forms.DockStyle.Fill,
            Margin = new System.Windows.Forms.Padding(3, 5, 3, 5)
        };
        var resetButton = new System.Windows.Forms.Button
        {
            Text = resetText,
            Dock = System.Windows.Forms.DockStyle.Fill,
            Margin = new System.Windows.Forms.Padding(3, 5, 3, 5)
        };
        browseButton.Click += browseHandler;
        resetButton.Click += resetHandler;
        layout.Controls.Add(label, 0, row);
        layout.Controls.Add(textBox, 1, row);
        layout.Controls.Add(browseButton, 2, row);
        layout.Controls.Add(resetButton, 3, row);
    }

    private void BrowseTemplate(object? sender, EventArgs eventArgs)
    {
        using var dialog = new System.Windows.Forms.OpenFileDialog
        {
            Title = "Select the BoM template workbook",
            Filter = "Excel workbook (*.xlsx)|*.xlsx",
            CheckFileExists = true,
            Multiselect = false,
            FileName = System.IO.Path.GetFileName(_templatePathBox.Text)
        };
        if (dialog.ShowDialog(this) == System.Windows.Forms.DialogResult.OK)
        {
            _templatePathBox.Text = dialog.FileName;
        }
    }

    private void BrowseOutputFolder(object? sender, EventArgs eventArgs)
    {
        using var dialog = new System.Windows.Forms.FolderBrowserDialog
        {
            Description = "Choose where generated BoM drafts are saved.",
            SelectedPath = System.IO.Directory.Exists(_outputDirectoryBox.Text)
                ? _outputDirectoryBox.Text
                : AddInSettings.DefaultBomOutputDirectory,
            ShowNewFolderButton = true
        };
        if (dialog.ShowDialog(this) == System.Windows.Forms.DialogResult.OK)
        {
            _outputDirectoryBox.Text = dialog.SelectedPath;
        }
    }

    private void BrowseDigitalImageOutputFolder(object? sender, EventArgs eventArgs)
    {
        using var dialog = new System.Windows.Forms.FolderBrowserDialog
        {
            Description = "Choose where digital images are saved.",
            SelectedPath = System.IO.Directory.Exists(_digitalImageOutputDirectoryBox.Text)
                ? _digitalImageOutputDirectoryBox.Text
                : AddInSettings.DefaultDigitalImageOutputDirectory,
            ShowNewFolderButton = true
        };
        if (dialog.ShowDialog(this) == System.Windows.Forms.DialogResult.OK)
        {
            _digitalImageOutputDirectoryBox.Text = dialog.SelectedPath;
        }
    }

    private void SaveSettings()
    {
        string templatePath = _templatePathBox.Text.Trim();
        if (!string.IsNullOrWhiteSpace(templatePath) && !System.IO.File.Exists(templatePath))
        {
            ShowValidationError("Select an existing Excel template, or choose Default to use the default template location.");
            _templatePathBox.Focus();
            return;
        }

        string outputDirectory;
        string digitalImageOutputDirectory;
        string selectedMaterialName = (_digitalImageMaterialBox.SelectedItem as ImageMaterialOption)?.Name ??
            AddInSettings.DefaultDigitalImageMaterial;
        string digitalImageMaterial = string.Equals(
            selectedMaterialName,
            AddInSettings.KeepCurrentAppearanceOption,
            StringComparison.OrdinalIgnoreCase)
            ? string.Empty
            : selectedMaterialName;
        try
        {
            outputDirectory = string.IsNullOrWhiteSpace(_outputDirectoryBox.Text)
                ? AddInSettings.DefaultBomOutputDirectory
                : System.IO.Path.GetFullPath(_outputDirectoryBox.Text.Trim());
            digitalImageOutputDirectory = string.IsNullOrWhiteSpace(_digitalImageOutputDirectoryBox.Text)
                ? AddInSettings.DefaultDigitalImageOutputDirectory
                : System.IO.Path.GetFullPath(_digitalImageOutputDirectoryBox.Text.Trim());
        }
        catch (Exception exception)
        {
            ShowValidationError($"An output folder is not a valid path.\n\n{exception.Message}");
            _digitalImageOutputDirectoryBox.Focus();
            return;
        }

        Settings = new AddInSettings
        {
            BomTemplatePath = templatePath,
            BomOutputDirectory = outputDirectory,
            DigitalImageOutputDirectory = digitalImageOutputDirectory,
            DigitalImageMaterial = digitalImageMaterial
        };
        DialogResult = System.Windows.Forms.DialogResult.OK;
        Close();
    }

    private void ShowValidationError(string message)
    {
        System.Windows.Forms.MessageBox.Show(
            this,
            message,
            "NJS Tools | Add-On Settings",
            System.Windows.Forms.MessageBoxButtons.OK,
            System.Windows.Forms.MessageBoxIcon.Warning);
    }
}
