using System.Runtime.InteropServices;
using Inventor;
using InventorApplication = Inventor.Application;

namespace NJS.InventorAddIn;

[Guid(ClassId)]
[ProgId(ProgId)]
[ComVisible(true)]
[ClassInterface(ClassInterfaceType.None)]
[ComDefaultInterface(typeof(ApplicationAddInServer))]
public sealed class StandardAddInServer : ApplicationAddInServer
{
    public const string ClassId = "D4E8B219-63C7-4A92-9B15-8E24F610C3A7";
    public const string ProgId = "NJS.InventorAddIn.StandardAddInServer";

    private InventorApplication? _application;
    private ButtonDefinition? _documentInfoButton;
    private ButtonDefinition? _generateBomButton;
    private ButtonDefinition? _printDraftButton;
    private ButtonDefinition? _digitalImageButton;
    private ButtonDefinition? _settingsButton;
    private AddInSettings _settings = AddInSettings.Load();

    public object Automation => null!;

    public void Activate(ApplicationAddInSite addInSiteObject, bool firstTime)
    {
        Log($"Activate started (FirstTime={firstTime}).");
        try
        {
            _application = addInSiteObject.Application;
            _settings = AddInSettings.Load();
            Log("Connected to Inventor application.");

            _documentInfoButton = _application.CommandManager.ControlDefinitions
                .AddButtonDefinition(
                    "Active Document Info",
                    "NJS_ActiveDocumentInfo",
                    CommandTypesEnum.kQueryOnlyCmdType,
                    ClassId,
                    "Show basic information about the active Inventor document.",
                    "Active Document Info",
                    null,
                    null,
                    ButtonDisplayEnum.kDisplayTextInLearningMode);
            _documentInfoButton.StandardIcon = CreateCommandIcon(generateBom: false, size: 16);
            _documentInfoButton.LargeIcon = CreateCommandIcon(generateBom: false, size: 32);
            Log("Created Active Document Info command.");

            _documentInfoButton.OnExecute += OnDocumentInfoExecute;
            _generateBomButton = _application.CommandManager.ControlDefinitions
                .AddButtonDefinition(
                    "Generate BoM",
                    "NJS_GenerateBOM",
                    CommandTypesEnum.kQueryOnlyCmdType,
                    ClassId,
                    "Generate a bill of materials using the configured Excel BoM template.",
                    "Generate BoM",
                    null,
                    null,
                    ButtonDisplayEnum.kDisplayTextInLearningMode);
            _generateBomButton.StandardIcon = CreateCommandIcon(generateBom: true, size: 16);
            _generateBomButton.LargeIcon = CreateCommandIcon(generateBom: true, size: 32);
            _generateBomButton.OnExecute += OnGenerateBomExecute;

            _printDraftButton = _application.CommandManager.ControlDefinitions
                .AddButtonDefinition(
                    "Print Draft",
                    "NJS_PrintDraft",
                    CommandTypesEnum.kQueryOnlyCmdType,
                    ClassId,
                    "Create a timestamped DRAFT PDF and open it for review or printing.",
                    "Print Draft",
                    null,
                    null,
                    ButtonDisplayEnum.kDisplayTextInLearningMode);
            _printDraftButton.StandardIcon = CreatePrintIcon(16);
            _printDraftButton.LargeIcon = CreatePrintIcon(32);
            _printDraftButton.OnExecute += OnPrintDraftExecute;

            _digitalImageButton = _application.CommandManager.ControlDefinitions
                .AddButtonDefinition(
                    "Digital Image",
                    "NJS_DigitalImage",
                    CommandTypesEnum.kQueryOnlyCmdType,
                    ClassId,
                    "Save the active part or assembly view as a transparent PNG image.",
                    "Digital Image",
                    null,
                    null,
                    ButtonDisplayEnum.kDisplayTextInLearningMode);
            _digitalImageButton.StandardIcon = CreateDigitalImageIcon(16);
            _digitalImageButton.LargeIcon = CreateDigitalImageIcon(32);
            _digitalImageButton.OnExecute += OnDigitalImageExecute;

            _settingsButton = _application.CommandManager.ControlDefinitions
                .AddButtonDefinition(
                    "Add-On Settings",
                    "NJS_AddOnSettings",
                    CommandTypesEnum.kQueryOnlyCmdType,
                    ClassId,
                    "Configure NJS Tools add-on preferences.",
                    "Add-On Settings",
                    null,
                    null,
                    ButtonDisplayEnum.kDisplayTextInLearningMode);
            _settingsButton.StandardIcon = CreateSettingsIcon(16);
            _settingsButton.LargeIcon = CreateSettingsIcon(32);
            _settingsButton.OnExecute += OnSettingsExecute;

            AddButtonToRibbon("Part");
            AddButtonToRibbon("Assembly");
            AddButtonToRibbon("Drawing");
            AddButtonToRibbon("Presentation");
            Log("Activate completed.");
        }
        catch (Exception exception)
        {
            Log($"Activate failed: {exception}");
            throw;
        }
    }

    public void Deactivate()
    {
        if (_documentInfoButton is not null)
        {
            _documentInfoButton.OnExecute -= OnDocumentInfoExecute;
            _documentInfoButton = null;
        }

        if (_generateBomButton is not null)
        {
            _generateBomButton.OnExecute -= OnGenerateBomExecute;
            _generateBomButton = null;
        }

        if (_printDraftButton is not null)
        {
            _printDraftButton.OnExecute -= OnPrintDraftExecute;
            _printDraftButton = null;
        }

        if (_digitalImageButton is not null)
        {
            _digitalImageButton.OnExecute -= OnDigitalImageExecute;
            _digitalImageButton = null;
        }

        if (_settingsButton is not null)
        {
            _settingsButton.OnExecute -= OnSettingsExecute;
            _settingsButton = null;
        }

        _application = null;
        GC.Collect();
        GC.WaitForPendingFinalizers();
        Log("Deactivate completed.");
    }

    public void ExecuteCommand(int commandID)
    {
    }

    private void AddButtonToRibbon(string ribbonName)
    {
        if (_application is null || _documentInfoButton is null)
        {
            return;
        }

        try
        {
            Ribbon ribbon = _application.UserInterfaceManager.Ribbons[ribbonName];
            RibbonTab toolsTab = ribbon.RibbonTabs["id_TabTools"];
            RibbonPanel panel = toolsTab.RibbonPanels.Add(
                "NJS Tools",
                $"NJS_{ribbonName}_Panel",
                ClassId);
            panel.CommandControls.AddButton(_documentInfoButton, true, true);
            if (ribbonName == "Assembly" && _generateBomButton is not null)
            {
                panel.CommandControls.AddButton(_generateBomButton, true, true);
            }
            if (ribbonName == "Drawing" && _printDraftButton is not null)
            {
                panel.CommandControls.AddButton(_printDraftButton, true, true);
            }
            if ((ribbonName == "Part" || ribbonName == "Assembly") && _digitalImageButton is not null)
            {
                panel.CommandControls.AddButton(_digitalImageButton, true, true);
            }
            if (_settingsButton is not null)
            {
                panel.CommandControls.AddButton(_settingsButton, true, true);
            }
            Log($"Added ribbon panel to '{ribbonName}'.");
        }
        catch (Exception exception)
        {
            Log($"Could not add the ribbon panel to '{ribbonName}': {exception}");
        }
    }

    private static void Log(string message)
    {
        try
        {
            string logDirectory = System.IO.Path.Combine(
                System.Environment.GetFolderPath(System.Environment.SpecialFolder.LocalApplicationData),
                "NJSTools",
                "InventorAddIn");
            System.IO.Directory.CreateDirectory(logDirectory);
            System.IO.File.AppendAllText(
                System.IO.Path.Combine(logDirectory, "activation.log"),
                $"{DateTimeOffset.Now:O} {message}{System.Environment.NewLine}");
        }
        catch
        {
        }
    }

    private static stdole.IPictureDisp CreateCommandIcon(bool generateBom, int size)
    {
        using var bitmap = new System.Drawing.Bitmap(size, size, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
        using (System.Drawing.Graphics graphics = System.Drawing.Graphics.FromImage(bitmap))
        using (var outline = new System.Drawing.Pen(System.Drawing.Color.FromArgb(43, 61, 78), 2.2f))
        using (var accent = new System.Drawing.SolidBrush(generateBom
                   ? System.Drawing.Color.FromArgb(0, 137, 123)
                   : System.Drawing.Color.FromArgb(231, 146, 45)))
        using (var paper = new System.Drawing.SolidBrush(System.Drawing.Color.White))
        using (var grid = new System.Drawing.Pen(System.Drawing.Color.FromArgb(104, 126, 144), 1.7f))
        {
            graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            graphics.Clear(System.Drawing.Color.Transparent);
            graphics.ScaleTransform(size / 32f, size / 32f);

            if (generateBom)
            {
                graphics.FillRectangle(paper, 4, 3, 24, 25);
                graphics.DrawRectangle(outline, 4, 3, 24, 25);
                graphics.FillRectangle(accent, 5, 4, 22, 6);
                graphics.DrawLine(grid, 5, 15, 27, 15);
                graphics.DrawLine(grid, 5, 20, 27, 20);
                graphics.DrawLine(grid, 5, 25, 27, 25);
                graphics.DrawLine(grid, 12, 10, 12, 28);
                graphics.DrawLine(grid, 20, 10, 20, 28);
                graphics.FillEllipse(accent, 20, 20, 11, 11);
                using var whitePen = new System.Drawing.Pen(System.Drawing.Color.White, 2.1f);
                graphics.DrawLine(whitePen, 22.5f, 25.5f, 25f, 28f);
                graphics.DrawLine(whitePen, 25f, 28f, 29f, 23f);
            }
            else
            {
                using var page = new System.Drawing.Drawing2D.GraphicsPath();
                page.AddPolygon(new[]
                {
                    new System.Drawing.PointF(6, 3),
                    new System.Drawing.PointF(20, 3),
                    new System.Drawing.PointF(27, 10),
                    new System.Drawing.PointF(27, 29),
                    new System.Drawing.PointF(6, 29)
                });
                graphics.FillPath(paper, page);
                graphics.DrawPath(outline, page);
                graphics.DrawLine(grid, 20, 4, 20, 10);
                graphics.DrawLine(grid, 20, 10, 26, 10);
                graphics.DrawLine(grid, 10, 14, 22, 14);
                graphics.DrawLine(grid, 10, 18, 18, 18);
                graphics.FillEllipse(accent, 18, 19, 13, 13);
                using var whitePen = new System.Drawing.Pen(System.Drawing.Color.White, 2.2f);
                graphics.DrawLine(whitePen, 24.5f, 24f, 24.5f, 28f);
                graphics.FillEllipse(paper, 23.4f, 21f, 2.2f, 2.2f);
            }
        }

        return PictureDispConverter.ToPictureDisp(bitmap);
    }

    private static stdole.IPictureDisp CreatePrintIcon(int size)
    {
        using var bitmap = new System.Drawing.Bitmap(size, size, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
        using (System.Drawing.Graphics graphics = System.Drawing.Graphics.FromImage(bitmap))
        using (var outline = new System.Drawing.Pen(System.Drawing.Color.FromArgb(43, 61, 78), 2.2f))
        using (var accent = new System.Drawing.SolidBrush(System.Drawing.Color.FromArgb(0, 137, 123)))
        using (var paper = new System.Drawing.SolidBrush(System.Drawing.Color.White))
        {
            graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            graphics.Clear(System.Drawing.Color.Transparent);
            graphics.ScaleTransform(size / 32f, size / 32f);
            graphics.FillRectangle(paper, 8, 3, 16, 12);
            graphics.DrawRectangle(outline, 8, 3, 16, 12);
            graphics.FillRectangle(accent, 5, 12, 22, 12);
            graphics.DrawRectangle(outline, 5, 12, 22, 12);
            graphics.FillRectangle(paper, 9, 20, 14, 9);
            graphics.DrawRectangle(outline, 9, 20, 14, 9);
            graphics.DrawLine(outline, 12, 24, 20, 24);
            graphics.DrawLine(outline, 12, 27, 18, 27);
            graphics.FillEllipse(paper, 21, 15, 2, 2);
        }

        return PictureDispConverter.ToPictureDisp(bitmap);
    }

    private static stdole.IPictureDisp CreateDigitalImageIcon(int size)
    {
        using var bitmap = new System.Drawing.Bitmap(size, size, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
        using (System.Drawing.Graphics graphics = System.Drawing.Graphics.FromImage(bitmap))
        using (var outline = new System.Drawing.Pen(System.Drawing.Color.FromArgb(43, 61, 78), 2.2f))
        using (var accent = new System.Drawing.SolidBrush(System.Drawing.Color.FromArgb(0, 137, 123)))
        using (var paper = new System.Drawing.SolidBrush(System.Drawing.Color.White))
        {
            graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            graphics.Clear(System.Drawing.Color.Transparent);
            graphics.ScaleTransform(size / 32f, size / 32f);
            graphics.FillRectangle(paper, 3, 5, 26, 22);
            graphics.DrawRectangle(outline, 3, 5, 26, 22);
            graphics.FillEllipse(accent, 20, 9, 5, 5);
            graphics.FillPolygon(accent, new[]
            {
                new System.Drawing.Point(5, 25),
                new System.Drawing.Point(13, 15),
                new System.Drawing.Point(18, 21),
                new System.Drawing.Point(22, 17),
                new System.Drawing.Point(28, 25)
            });
        }

        return PictureDispConverter.ToPictureDisp(bitmap);
    }

    private static stdole.IPictureDisp CreateSettingsIcon(int size)
    {
        using var bitmap = new System.Drawing.Bitmap(size, size, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
        using (System.Drawing.Graphics graphics = System.Drawing.Graphics.FromImage(bitmap))
        using (var outline = new System.Drawing.Pen(System.Drawing.Color.FromArgb(43, 61, 78), 2.4f))
        using (var accent = new System.Drawing.SolidBrush(System.Drawing.Color.FromArgb(0, 137, 123)))
        {
            graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            graphics.Clear(System.Drawing.Color.Transparent);
            graphics.ScaleTransform(size / 32f, size / 32f);
            graphics.DrawLine(outline, 16, 3, 16, 8);
            graphics.DrawLine(outline, 16, 24, 16, 29);
            graphics.DrawLine(outline, 3, 16, 8, 16);
            graphics.DrawLine(outline, 24, 16, 29, 16);
            graphics.DrawLine(outline, 6.8f, 6.8f, 10.3f, 10.3f);
            graphics.DrawLine(outline, 21.7f, 21.7f, 25.2f, 25.2f);
            graphics.DrawLine(outline, 25.2f, 6.8f, 21.7f, 10.3f);
            graphics.DrawLine(outline, 10.3f, 21.7f, 6.8f, 25.2f);
            graphics.FillEllipse(accent, 8, 8, 16, 16);
            graphics.DrawEllipse(outline, 8, 8, 16, 16);
            graphics.FillEllipse(System.Drawing.Brushes.White, 13, 13, 6, 6);
            graphics.DrawEllipse(outline, 13, 13, 6, 6);
        }

        return PictureDispConverter.ToPictureDisp(bitmap);
    }

    private sealed class PictureDispConverter : System.Windows.Forms.AxHost
    {
        private PictureDispConverter() : base(string.Empty)
        {
        }

        public static stdole.IPictureDisp ToPictureDisp(System.Drawing.Image image) =>
            (stdole.IPictureDisp)GetIPictureDispFromPicture(image);
    }

    private void OnDocumentInfoExecute(NameValueMap context)
    {
        if (_application is null)
        {
            return;
        }

        try
        {
            Document document = _application.ActiveDocument;
            string message = $"Document: {document.DisplayName}\n" +
                             $"Type: {document.DocumentType}\n" +
                             $"File: {(string.IsNullOrWhiteSpace(document.FullFileName) ? "(not saved)" : document.FullFileName)}";
            _application.StatusBarText = message.Replace('\n', ' ');
            System.Windows.Forms.MessageBox.Show(
                message,
                "NJS Tools | Active Document",
                System.Windows.Forms.MessageBoxButtons.OK,
                System.Windows.Forms.MessageBoxIcon.Information);
        }
        catch (Exception exception)
        {
            System.Diagnostics.Debug.WriteLine($"NJS Tools: document info command failed: {exception}");
        }
    }

    private void OnGenerateBomExecute(NameValueMap context)
    {
        if (_application is null)
        {
            return;
        }

        try
        {
            Document document = _application.ActiveDocument;
            if (document.DocumentType != DocumentTypeEnum.kAssemblyDocumentObject)
            {
                System.Windows.Forms.MessageBox.Show(
                    "Open an assembly document to generate its bill of materials.",
                    "NJS Tools | Generate BoM",
                    System.Windows.Forms.MessageBoxButtons.OK,
                    System.Windows.Forms.MessageBoxIcon.Warning);
                return;
            }

            var assemblyDocument = (AssemblyDocument)document;
            List<BomLine> bomLines = ReadStructuredBom(assemblyDocument);
            if (bomLines.Count == 0)
            {
                throw new InvalidOperationException("The assembly's structured BOM contains no rows.");
            }

            string assemblyNumberDefault = GetInventorProperty(document, "Design Tracking Properties", "Part Number");
            string revisionDefault = GetInventorProperty(document, "Design Tracking Properties", "Revision Number");
            using var detailsDialog = new BomDetailsDialog(assemblyNumberDefault, revisionDefault);
            if (detailsDialog.ShowDialog() != System.Windows.Forms.DialogResult.OK)
            {
                return;
            }

            string templatePath = ResolveBomTemplate();
            if (string.IsNullOrWhiteSpace(templatePath))
            {
                return;
            }

            string draftDirectory = string.IsNullOrWhiteSpace(_settings.BomOutputDirectory)
                ? AddInSettings.DefaultBomOutputDirectory
                : _settings.BomOutputDirectory;
            System.IO.Directory.CreateDirectory(draftDirectory);
            string outputPath = System.IO.Path.GetFullPath(
                System.IO.Path.Combine(draftDirectory, detailsDialog.GeneratedFileName));
            if (string.Equals(outputPath, System.IO.Path.GetFullPath(templatePath), StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("The output path cannot be the source template path.");
            }

            if (System.IO.File.Exists(outputPath))
            {
                throw new IOException(
                    $"A draft BoM with this name already exists and was not overwritten:\n{outputPath}\n\nChange the revision or archive the existing draft before generating another.");
            }

            string temporaryPath = System.IO.Path.Combine(
                System.IO.Path.GetDirectoryName(outputPath)!,
                $".{System.IO.Path.GetFileNameWithoutExtension(outputPath)}.{Guid.NewGuid():N}.tmp.xlsx");
            try
            {
                System.IO.File.Copy(templatePath, temporaryPath, false);
                PopulateBomTemplate(
                    temporaryPath,
                    document,
                    bomLines,
                    detailsDialog.AssemblyNumber,
                    detailsDialog.Revision);
                System.IO.File.Move(temporaryPath, outputPath);
            }
            finally
            {
                if (System.IO.File.Exists(temporaryPath))
                {
                    try
                    {
                        System.IO.File.Delete(temporaryPath);
                    }
                    catch (Exception cleanupException)
                    {
                        Log($"Could not remove temporary BoM workbook '{temporaryPath}': {cleanupException.Message}");
                    }
                }
            }

            System.Windows.Forms.MessageBox.Show(
                $"Bill of materials saved to the Draft folder:\n{outputPath}",
                "NJS Tools | Generate BoM",
                System.Windows.Forms.MessageBoxButtons.OK,
                System.Windows.Forms.MessageBoxIcon.Information);
            Log($"Generated template BOM for '{document.DisplayName}' at '{outputPath}' with {bomLines.Count} rows; assembly='{detailsDialog.AssemblyNumber}', revision='{detailsDialog.Revision}'.");
        }
        catch (Exception exception)
        {
            Log($"Generate BoM failed: {exception}");
            System.Windows.Forms.MessageBox.Show(
                $"Could not generate the bill of materials.\n\n{exception.Message}",
                "NJS Tools | Generate BoM",
                System.Windows.Forms.MessageBoxButtons.OK,
                System.Windows.Forms.MessageBoxIcon.Error);
        }
    }

    private void OnPrintDraftExecute(NameValueMap context)
    {
        if (_application is null)
        {
            return;
        }

        try
        {
            Document document = _application.ActiveDocument;
            if (document.DocumentType != DocumentTypeEnum.kDrawingDocumentObject)
            {
                System.Windows.Forms.MessageBox.Show(
                    "Open a drawing document to print a draft.",
                    "NJS Tools | Print Draft",
                    System.Windows.Forms.MessageBoxButtons.OK,
                    System.Windows.Forms.MessageBoxIcon.Warning);
                return;
            }

            var drawingDocument = (DrawingDocument)document;
            DateTimeOffset printTime = DateTimeOffset.UtcNow;
            string username = System.Environment.UserName;
            string timestamp = printTime.ToString(
                "yyyy-MM-dd HH:mm:ss 'UTC'",
                System.Globalization.CultureInfo.InvariantCulture);
            string filenameTimestamp = printTime.ToString(
                "yyyyMMdd-HHmmss'Z'",
                System.Globalization.CultureInfo.InvariantCulture);
            string documentPath = document.FullFileName;
            string originalName = System.IO.Path.GetFileNameWithoutExtension(
                string.IsNullOrWhiteSpace(documentPath) ? document.DisplayName : documentPath);
            string safeUsername = SanitizeFileNamePart(username);
            string suggestedFileName = $"{originalName}-DRAFT-{safeUsername}-{filenameTimestamp}.pdf";
            string initialDirectory = System.IO.Path.GetDirectoryName(documentPath) ??
                System.Environment.GetFolderPath(System.Environment.SpecialFolder.MyDocuments);

            using var saveDialog = new System.Windows.Forms.SaveFileDialog
            {
                AddExtension = true,
                DefaultExt = "pdf",
                FileName = suggestedFileName,
                Filter = "PDF document (*.pdf)|*.pdf",
                InitialDirectory = System.IO.Directory.Exists(initialDirectory)
                    ? initialDirectory
                    : System.Environment.GetFolderPath(System.Environment.SpecialFolder.MyDocuments),
                OverwritePrompt = true,
                Title = "Save Draft PDF"
            };
            if (saveDialog.ShowDialog() != System.Windows.Forms.DialogResult.OK)
            {
                return;
            }

            TranslatorAddIn pdfTranslator = (TranslatorAddIn)_application.ApplicationAddIns
                .ItemById["{0AC6FD96-2F4D-42CE-8BE0-8AEA580399E4}"];
            if (!pdfTranslator.SupportsSaveCopyAs)
            {
                throw new InvalidOperationException("The Inventor PDF translator is not available.");
            }

            TranslationContext translationContext = _application.TransientObjects.CreateTranslationContext();
            translationContext.Type = IOMechanismEnum.kFileBrowseIOMechanism;
            NameValueMap exportOptions = _application.TransientObjects.CreateNameValueMap();
            DataMedium outputData = _application.TransientObjects.CreateDataMedium();
            outputData.FileName = saveDialog.FileName;
            bool wasDirty = document.Dirty;
            Transaction transaction = _application.TransactionManager.StartTransaction((_Document)document, "Print Draft");
            try
            {
                AddDraftWatermarks(drawingDocument, _application, username, timestamp);
                drawingDocument.Update();
                if (pdfTranslator.HasSaveCopyAsOptions[document, translationContext, exportOptions])
                {
                    pdfTranslator.ShowSaveCopyAsOptions(document, translationContext, exportOptions);
                }

                pdfTranslator.SaveCopyAs(document, translationContext, exportOptions, outputData);
            }
            finally
            {
                try
                {
                    transaction.Abort();
                }
                finally
                {
                    document.Dirty = wasDirty;
                }
            }

            try
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(saveDialog.FileName)
                {
                    UseShellExecute = true
                });
            }
            catch (Exception openException)
            {
                Log($"Draft PDF was saved but could not be opened: {openException}");
                System.Windows.Forms.MessageBox.Show(
                    $"Draft PDF saved to:\n{saveDialog.FileName}\n\nIt could not be opened automatically: {openException.Message}",
                    "NJS Tools | Print Draft",
                    System.Windows.Forms.MessageBoxButtons.OK,
                    System.Windows.Forms.MessageBoxIcon.Warning);
                return;
            }

            Log($"Generated and opened draft PDF '{saveDialog.FileName}'; temporary watermarks were rolled back.");
        }
        catch (Exception exception)
        {
            Log($"Print Draft failed: {exception}");
            System.Windows.Forms.MessageBox.Show(
                $"Could not prepare the drawing draft for printing.\n\n{exception.Message}",
                "NJS Tools | Print Draft",
                System.Windows.Forms.MessageBoxButtons.OK,
                System.Windows.Forms.MessageBoxIcon.Error);
        }
    }

    private void OnDigitalImageExecute(NameValueMap context)
    {
        if (_application is null)
        {
            return;
        }

        try
        {
            Document document = _application.ActiveDocument;
            if (document.DocumentType != DocumentTypeEnum.kPartDocumentObject &&
                document.DocumentType != DocumentTypeEnum.kAssemblyDocumentObject)
            {
                System.Windows.Forms.MessageBox.Show(
                    "Open a part or assembly document to save a digital image.",
                    "NJS Tools | Digital Image",
                    System.Windows.Forms.MessageBoxButtons.OK,
                    System.Windows.Forms.MessageBoxIcon.Warning);
                return;
            }

            string documentPath = document.FullFileName;
            string originalName = System.IO.Path.GetFileNameWithoutExtension(
                string.IsNullOrWhiteSpace(documentPath) ? document.DisplayName : documentPath);
            string imageOutputDirectory = string.IsNullOrWhiteSpace(_settings.DigitalImageOutputDirectory)
                ? AddInSettings.DefaultDigitalImageOutputDirectory
                : _settings.DigitalImageOutputDirectory;
            System.IO.Directory.CreateDirectory(imageOutputDirectory);
            using var saveDialog = new System.Windows.Forms.SaveFileDialog
            {
                AddExtension = true,
                DefaultExt = "png",
                FileName = $"{originalName}.png",
                Filter = "PNG image (*.png)|*.png",
                InitialDirectory = imageOutputDirectory,
                OverwritePrompt = true,
                Title = "Save Digital Image"
            };
            if (saveDialog.ShowDialog() != System.Windows.Forms.DialogResult.OK)
            {
                return;
            }

            Inventor.View activeView = _application.ActiveView;
            Camera camera = activeView.Camera;
            camera.ViewOrientationType = ViewOrientationTypeEnum.kTopViewOrientation;
            camera.ApplyWithoutTransition();

            bool applyCustomAppearance = !string.IsNullOrWhiteSpace(_settings.DigitalImageMaterial) &&
                !string.Equals(
                    _settings.DigitalImageMaterial,
                    AddInSettings.KeepCurrentAppearanceOption,
                    StringComparison.OrdinalIgnoreCase);

            Transaction transaction = _application.TransactionManager.StartTransaction(
                (_Document)document,
                applyCustomAppearance
                    ? $"Digital Image - Temporary {_settings.DigitalImageMaterial} Appearance"
                    : "Digital Image");
            try
            {
                if (applyCustomAppearance)
                {
                    Asset imageMaterialAppearance = GetImageAppearance(document, _settings.DigitalImageMaterial);
                    if (document is PartDocument partDocument)
                    {
                        partDocument.ActiveAppearance = imageMaterialAppearance;
                    }
                    else
                    {
                        var assemblyDocument = (AssemblyDocument)document;
                        ComponentOccurrencesEnumerator occurrences =
                            assemblyDocument.ComponentDefinition.Occurrences.AllLeafOccurrences;
                        for (int index = 1; index <= occurrences.Count; index++)
                        {
                            occurrences[index].Appearance = imageMaterialAppearance;
                        }
                    }
                }

                activeView.Update();
                camera.Fit();
                camera.ApplyWithoutTransition();
                activeView.Update();
                NameValueMap options = _application.TransientObjects.CreateNameValueMap();
                options.Add("TransparentBackground", true);
                activeView.SaveAsBitmapWithOptions(saveDialog.FileName, 0, 0, options);
            }
            finally
            {
                transaction.Abort();
                activeView.Update();
            }

            System.Windows.Forms.MessageBox.Show(
                $"Digital image saved to:\n{saveDialog.FileName}",
                "NJS Tools | Digital Image",
                System.Windows.Forms.MessageBoxButtons.OK,
                System.Windows.Forms.MessageBoxIcon.Information);
            Log($"Saved digital image for '{document.DisplayName}' to '{saveDialog.FileName}'.");
        }
        catch (Exception exception)
        {
            Log($"Digital Image failed: {exception}");
            System.Windows.Forms.MessageBox.Show(
                $"Could not save the digital image.\n\n{exception.Message}",
                "NJS Tools | Digital Image",
                System.Windows.Forms.MessageBoxButtons.OK,
                System.Windows.Forms.MessageBoxIcon.Error);
        }
    }

    private IReadOnlyList<string> GetAvailableImageMaterials()
    {
        var materialNames = new SortedSet<string>(StringComparer.OrdinalIgnoreCase);
        if (_application is null)
        {
            return materialNames.ToArray();
        }

        foreach (AssetLibrary library in GetImageMaterialLibraries())
        {
            try
            {
                AssetsEnumerator materials = library.MaterialAssets;
                for (int materialIndex = 1; materialIndex <= materials.Count; materialIndex++)
                {
                    AddAssetName((Asset)materials[materialIndex], materialNames);
                }

                AssetsEnumerator appearances = library.AppearanceAssets;
                for (int appearanceIndex = 1; appearanceIndex <= appearances.Count; appearanceIndex++)
                {
                    AddAssetName((Asset)appearances[appearanceIndex], materialNames);
                }
            }
            catch (Exception exception)
            {
                Log($"Could not list image assets from Inventor library '{library.DisplayName}': {exception.Message}");
            }
        }

        try
        {
            Document document = _application.ActiveDocument;
            AssetsEnumerator? documentMaterials = document.DocumentType == DocumentTypeEnum.kPartDocumentObject
                ? ((PartDocument)document).MaterialAssets
                : document.DocumentType == DocumentTypeEnum.kAssemblyDocumentObject
                    ? ((AssemblyDocument)document).MaterialAssets
                    : null;
            if (documentMaterials is not null)
            {
                for (int index = 1; index <= documentMaterials.Count; index++)
                {
                    AddAssetName((Asset)documentMaterials[index], materialNames);
                }
            }

            AssetsEnumerator? documentAppearances = document.DocumentType == DocumentTypeEnum.kPartDocumentObject
                ? ((PartDocument)document).AppearanceAssets
                : document.DocumentType == DocumentTypeEnum.kAssemblyDocumentObject
                    ? ((AssemblyDocument)document).AppearanceAssets
                    : null;
            if (documentAppearances is not null)
            {
                for (int index = 1; index <= documentAppearances.Count; index++)
                {
                    AddAssetName((Asset)documentAppearances[index], materialNames);
                }
            }
        }
        catch (Exception exception)
        {
            Log($"Could not list materials from the active document: {exception.Message}");
        }

        return materialNames.ToArray();
    }

    private IReadOnlyList<AssetLibrary> GetImageMaterialLibraries()
    {
        if (_application is null)
        {
            return Array.Empty<AssetLibrary>();
        }

        var materialLibraries = new List<AssetLibrary>();
        var libraryPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        void AddLibrary(AssetLibrary library)
        {
            string fullFileName = library.FullFileName;
            if (!string.IsNullOrWhiteSpace(fullFileName) && libraryPaths.Add(fullFileName))
            {
                materialLibraries.Add(library);
            }
        }

        AssetLibraries loadedLibraries = _application.AssetLibraries;
        for (int index = 1; index <= loadedLibraries.Count; index++)
        {
            try
            {
                AddLibrary(loadedLibraries[index]);
            }
            catch (Exception exception)
            {
                Log($"Could not inspect loaded Inventor library {index}: {exception.Message}");
            }
        }

        try
        {
            ProjectAssetLibraries projectLibraries =
                _application.DesignProjectManager.ActiveDesignProject.MaterialLibraries;
            for (int index = 1; index <= projectLibraries.Count; index++)
            {
                ProjectAssetLibrary projectLibrary = projectLibraries[index];
                if (libraryPaths.Contains(projectLibrary.LibraryFilename))
                {
                    continue;
                }

                try
                {
                    AddLibrary(_application.AssetLibraries.Open(projectLibrary.LibraryFilename));
                }
                catch (Exception exception)
                {
                    Log($"Could not open project material library '{projectLibrary.LibraryFilename}': {exception.Message}");
                }
            }
        }
        catch (Exception exception)
        {
            Log($"Could not enumerate material libraries in the active Inventor project: {exception.Message}");
        }

        return materialLibraries;
    }

    private Asset GetImageAppearance(Document document, string assetName)
    {
        AssetsEnumerator documentMaterials = document.DocumentType == DocumentTypeEnum.kPartDocumentObject
            ? ((PartDocument)document).MaterialAssets
            : ((AssemblyDocument)document).MaterialAssets;
        for (int index = 1; index <= documentMaterials.Count; index++)
        {
            var material = (MaterialAsset)documentMaterials[index];
            if (MatchesAssetName(material, assetName))
            {
                return material.AppearanceAsset;
            }
        }

        AssetsEnumerator documentAppearances = document.DocumentType == DocumentTypeEnum.kPartDocumentObject
            ? ((PartDocument)document).AppearanceAssets
            : ((AssemblyDocument)document).AppearanceAssets;
        for (int index = 1; index <= documentAppearances.Count; index++)
        {
            Asset appearance = (Asset)documentAppearances[index];
            if (MatchesAssetName(appearance, assetName))
            {
                return appearance;
            }
        }

        foreach (AssetLibrary library in GetImageMaterialLibraries())
        {
            AssetsEnumerator libraryMaterials = library.MaterialAssets;
            for (int materialIndex = 1; materialIndex <= libraryMaterials.Count; materialIndex++)
            {
                var material = (MaterialAsset)libraryMaterials[materialIndex];
                if (MatchesAssetName(material, assetName))
                {
                    return ((MaterialAsset)material.CopyTo(document, false)).AppearanceAsset;
                }
            }

            AssetsEnumerator libraryAppearances = library.AppearanceAssets;
            for (int appearanceIndex = 1; appearanceIndex <= libraryAppearances.Count; appearanceIndex++)
            {
                Asset appearance = (Asset)libraryAppearances[appearanceIndex];
                if (MatchesAssetName(appearance, assetName))
                {
                    return (Asset)appearance.CopyTo(document, false);
                }
            }
        }

        throw new InvalidOperationException(
            $"The '{assetName}' material or appearance was not found in the active document or any loaded or project Inventor library.");
    }

    private static void AddAssetName(Asset asset, ISet<string> names)
    {
        string name = string.IsNullOrWhiteSpace(asset.DisplayName) ? asset.Name : asset.DisplayName;
        if (!string.IsNullOrWhiteSpace(name))
        {
            names.Add(name);
        }
    }

    private static bool MatchesAssetName(Asset asset, string assetName) =>
        string.Equals(asset.DisplayName, assetName, StringComparison.OrdinalIgnoreCase) ||
        string.Equals(asset.Name, assetName, StringComparison.OrdinalIgnoreCase);

    private static bool MatchesAssetName(MaterialAsset material, string assetName) =>
        string.Equals(material.DisplayName, assetName, StringComparison.OrdinalIgnoreCase) ||
        string.Equals(material.Name, assetName, StringComparison.OrdinalIgnoreCase);

    private static string SanitizeFileNamePart(string value)
    {
        foreach (char invalidCharacter in System.IO.Path.GetInvalidFileNameChars())
        {
            value = value.Replace(invalidCharacter, '_');
        }

        return value.Trim();
    }

    private static void AddDraftWatermarks(
        DrawingDocument drawingDocument,
        InventorApplication application,
        string username,
        string timestamp)
    {
        Inventor.Color watermarkColor = application.TransientObjects.CreateColor(248, 205, 205, 1.0);
        username = System.Security.SecurityElement.Escape(username) ?? username;
        for (int index = 1; index <= drawingDocument.Sheets.Count; index++)
        {
            Sheet sheet = drawingDocument.Sheets[index];
            double centerX = sheet.Width * 0.22 - 7.0;
            double centerY = sheet.Height * 0.065 + 2.0;
            double fontSize = Math.Clamp(Math.Min(sheet.Width, sheet.Height) * 0.05, 0.8, 1.5);
            if (sheet.TitleBlock is TitleBlock titleBlock)
            {
                try
                {
                    Box2d titleBlockBounds = titleBlock.RangeBox;
                    double titleBlockLeft = titleBlockBounds.MinPoint.X;
                    double titleBlockHeight = titleBlockBounds.MaxPoint.Y - titleBlockBounds.MinPoint.Y;
                    if (titleBlockLeft > sheet.Width * 0.25 && titleBlockHeight > 0)
                    {
                        centerX = titleBlockLeft * 0.35 - 7.0;
                        centerY = titleBlockBounds.MinPoint.Y + titleBlockHeight * 0.45 + 2.0;
                        fontSize = Math.Clamp(titleBlockHeight * 0.3, 0.8, 1.5);
                    }
                }
                catch (COMException exception)
                {
                    Log($"Could not read title block bounds on sheet '{sheet.Name}'; using fallback draft watermark position: {exception.Message}");
                }
            }

            System.Globalization.CultureInfo invariantCulture = System.Globalization.CultureInfo.InvariantCulture;
            string formattedText = $"<StyleOverride FontSize='{fontSize.ToString(invariantCulture)}' Bold='True'>DRAFT</StyleOverride>";
            double metadataFontSize = Math.Clamp(fontSize * 0.3, 0.3, 0.45);
            string formattedMetadata =
                $"<StyleOverride FontSize='{metadataFontSize.ToString(invariantCulture)}'>Printed by {username} | {timestamp}</StyleOverride>";
            Point2d watermarkPosition = application.TransientGeometry.CreatePoint2d(
                centerX,
                centerY + fontSize * 0.25);
            Point2d metadataPosition = application.TransientGeometry.CreatePoint2d(
                centerX,
                centerY - fontSize * 1.0);
            GeneralNote watermark = sheet.DrawingNotes.GeneralNotes.AddFitted(watermarkPosition, formattedText, null);
            watermark.Color = watermarkColor;
            watermark.HorizontalJustification = HorizontalTextAlignmentEnum.kAlignTextCenter;
            watermark.VerticalJustification = VerticalTextAlignmentEnum.kAlignTextMiddle;
            watermark.Rotation = 0;

            GeneralNote metadata = sheet.DrawingNotes.GeneralNotes.AddFitted(metadataPosition, formattedMetadata, null);
            metadata.Color = watermarkColor;
            metadata.HorizontalJustification = HorizontalTextAlignmentEnum.kAlignTextCenter;
            metadata.VerticalJustification = VerticalTextAlignmentEnum.kAlignTextMiddle;
            metadata.Rotation = 0;
        }
    }

    private static List<BomLine> ReadStructuredBom(AssemblyDocument assemblyDocument)
    {
        BOM bom = assemblyDocument.ComponentDefinition.BOM;
        bool originalStructuredEnabled = bom.StructuredViewEnabled;
        bool originalFirstLevelOnly = bom.StructuredViewFirstLevelOnly;
        var lines = new List<BomLine>();

        try
        {
            bom.StructuredViewEnabled = true;
            bom.StructuredViewFirstLevelOnly = false;

            BOMView? structuredView = null;
            for (int index = 1; index <= bom.BOMViews.Count; index++)
            {
                BOMView candidate = bom.BOMViews[index];
                if (candidate.ViewType == BOMViewTypeEnum.kStructuredBOMViewType)
                {
                    structuredView = candidate;
                    break;
                }
            }

            if (structuredView is null)
            {
                throw new InvalidOperationException("Inventor did not provide a structured BOM view for this assembly.");
            }

            AppendBomRows(structuredView.BOMRows, lines);
        }
        finally
        {
            bom.StructuredViewFirstLevelOnly = originalFirstLevelOnly;
            bom.StructuredViewEnabled = originalStructuredEnabled;
        }

        return lines;
    }

    private static void AppendBomRows(BOMRowsEnumerator rows, List<BomLine> lines)
    {
        for (int index = 1; index <= rows.Count; index++)
        {
            BOMRow row = rows[index];
            string partNumber = string.Empty;
            try
            {
                if (row.ComponentDefinitions.Count > 0 && row.ComponentDefinitions[1].Document is Document componentDocument)
                {
                    partNumber = GetInventorProperty(componentDocument, "Design Tracking Properties", "Part Number");
                }
            }
            catch (Exception exception)
            {
                Log($"Could not read part number for BOM item '{row.ItemNumber}': {exception.Message}");
            }

            lines.Add(new BomLine(row.ItemNumber, partNumber, row.ItemQuantity));
            if (row.ChildRows is BOMRowsEnumerator childRows)
            {
                AppendBomRows(childRows, lines);
            }
        }
    }

    private static string GetInventorProperty(Document document, string propertySetName, string propertyName)
    {
        try
        {
            PropertySets propertySets = document.PropertySets;
            PropertySet propertySet = propertySets[propertySetName];
            Property property = propertySet[propertyName];
            return Convert.ToString(property.Value) ?? string.Empty;
        }
        catch
        {
            return string.Empty;
        }
    }

    private string ResolveBomTemplate()
    {
        if (!string.IsNullOrWhiteSpace(_settings.BomTemplatePath) && System.IO.File.Exists(_settings.BomTemplatePath))
        {
            return _settings.BomTemplatePath;
        }

        string defaultPath = AddInSettings.DefaultBomTemplatePath;
        if (System.IO.File.Exists(defaultPath))
        {
            return defaultPath;
        }

        using var openDialog = new System.Windows.Forms.OpenFileDialog
        {
            Title = "Locate the Excel BoM template",
            Filter = "Excel workbook (*.xlsx)|*.xlsx",
            CheckFileExists = true,
            Multiselect = false,
            FileName = "BoM Template.xlsx"
        };

        if (openDialog.ShowDialog() != System.Windows.Forms.DialogResult.OK)
        {
            return string.Empty;
        }

        _settings.BomTemplatePath = openDialog.FileName;
        _settings.Save();
        return _settings.BomTemplatePath;
    }

    private void OnSettingsExecute(NameValueMap context)
    {
        using var dialog = new AddInSettingsDialog(_settings, GetAvailableImageMaterials());
        if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
        {
            _settings = dialog.Settings;
            _settings.Save();
            Log("Add-on settings saved.");
        }
    }

    private static void PopulateBomTemplate(
        string outputPath,
        Document assemblyDocument,
        IReadOnlyList<BomLine> bomLines,
        string assemblyNumber,
        string revision)
    {
        Type? excelType = Type.GetTypeFromProgID("Excel.Application");
        if (excelType is null)
        {
            throw new InvalidOperationException("Microsoft Excel desktop is required to fill the BoM workbook template.");
        }

        object? excelObject = null;
        object? workbookObject = null;
        object? workbooksObject = null;
        object? worksheetsObject = null;
        object? sheetObject = null;
        object? tableObject = null;
        object? tableRowsObject = null;
        try
        {
            excelObject = Activator.CreateInstance(excelType)
                ?? throw new InvalidOperationException("Could not start Microsoft Excel.");
            dynamic excel = excelObject;
            excel.Visible = false;
            excel.DisplayAlerts = false;
            excel.AskToUpdateLinks = false;
            excel.EnableEvents = false;

            workbooksObject = excel.Workbooks;
            dynamic workbooks = workbooksObject;
            workbookObject = workbooks.Open(outputPath, 0, false);
            dynamic workbook = workbookObject;
            worksheetsObject = workbook.Worksheets;
            dynamic worksheets = worksheetsObject;
            sheetObject = worksheets.Item["BoM Template"];
            dynamic sheet = sheetObject;
            object listObjectsObject = sheet.ListObjects;
            dynamic listObjects = listObjectsObject;
            tableObject = listObjects.Item["tblBOM"];
            Marshal.FinalReleaseComObject(listObjectsObject);
            dynamic table = tableObject;

            string assemblyName = GetInventorProperty(assemblyDocument, "Design Tracking Properties", "Description");
            if (string.IsNullOrWhiteSpace(assemblyName))
            {
                assemblyName = assemblyDocument.DisplayName;
            }

            object worksheetCellsObject = sheet.Cells;
            try
            {
                dynamic worksheetCells = worksheetCellsObject;
                SetTemplateCell(worksheetCells, 2, 3, assemblyNumber);
                SetTemplateCell(worksheetCells, 3, 3, assemblyName);
                SetTemplateCell(worksheetCells, 4, 3, GetInventorProperty(assemblyDocument, "Summary Information", "Comments"));
                SetTemplateCell(worksheetCells, 5, 3, revision);
            }
            finally
            {
                Marshal.FinalReleaseComObject(worksheetCellsObject);
            }

            int lastTableRow = 10 + bomLines.Count;
            object tableRangeObject = sheet.Range[$"A10:F{lastTableRow}"];
            try
            {
                table.Resize(tableRangeObject);
            }
            finally
            {
                Marshal.FinalReleaseComObject(tableRangeObject);
            }
            Log($"Resized BoM table to {bomLines.Count} data rows.");

            tableRowsObject = table.ListRows;
            dynamic tableRows = tableRowsObject;
            if ((int)tableRows.Count != bomLines.Count)
            {
                throw new InvalidOperationException(
                    $"Excel resized the BoM table to {tableRows.Count} rows; expected {bomLines.Count}.");
            }

            for (int index = 0; index < bomLines.Count; index++)
            {
                BomLine line = bomLines[index];
                object rowObject = tableRows.Item[index + 1];
                object rangeObject = ((dynamic)rowObject).Range;
                object cellsObject = ((dynamic)rangeObject).Cells;
                try
                {
                    dynamic cells = cellsObject;
                    SetTemplateCell(cells, 1, 1, line.ItemNumber);
                    SetTemplateCell(cells, 1, 3, line.PartNumber);
                    SetTemplateCell(cells, 1, 5, line.Quantity);
                    ClearTemplateCell(cells, 1, 6);
                }
                finally
                {
                    Marshal.FinalReleaseComObject(cellsObject);
                    Marshal.FinalReleaseComObject(rangeObject);
                    Marshal.FinalReleaseComObject(rowObject);
                }
            }

            workbook.Save();
            workbook.Close(false);
            Marshal.FinalReleaseComObject(workbookObject);
            workbookObject = null;
        }
        finally
        {
            if (workbookObject is not null)
            {
                try
                {
                    ((dynamic)workbookObject).Close(false);
                }
                catch
                {
                }

                Marshal.FinalReleaseComObject(workbookObject);
            }

            if (tableRowsObject is not null)
            {
                Marshal.FinalReleaseComObject(tableRowsObject);
            }

            if (tableObject is not null)
            {
                Marshal.FinalReleaseComObject(tableObject);
            }

            if (sheetObject is not null)
            {
                Marshal.FinalReleaseComObject(sheetObject);
            }

            if (worksheetsObject is not null)
            {
                Marshal.FinalReleaseComObject(worksheetsObject);
            }

            if (workbooksObject is not null)
            {
                Marshal.FinalReleaseComObject(workbooksObject);
            }

            if (excelObject is not null)
            {
                try
                {
                    ((dynamic)excelObject).Quit();
                }
                catch
                {
                }

                Marshal.FinalReleaseComObject(excelObject);
            }
        }
    }

    private static void SetTemplateCell(dynamic cells, int row, int column, object value)
    {
        if (value is string text && string.IsNullOrWhiteSpace(text))
        {
            return;
        }

        object cellObject = cells[row, column];
        try
        {
            ((dynamic)cellObject).Value2 = value;
        }
        finally
        {
            Marshal.FinalReleaseComObject(cellObject);
        }
    }

    private static void ClearTemplateCell(dynamic cells, int row, int column)
    {
        object cellObject = cells[row, column];
        try
        {
            ((dynamic)cellObject).ClearContents();
        }
        finally
        {
            Marshal.FinalReleaseComObject(cellObject);
        }
    }

    private sealed record BomLine(string ItemNumber, string PartNumber, int Quantity);

    private sealed class BomDetailsDialog : System.Windows.Forms.Form
    {
        private readonly System.Windows.Forms.TextBox _assemblyNumberBox = new();
        private readonly System.Windows.Forms.TextBox _revisionBox = new();
        private readonly System.Windows.Forms.Label _fileNamePreview = new();
        private readonly System.Windows.Forms.Label _validationMessage = new();

        public string AssemblyNumber => _assemblyNumberBox.Text.Trim();
        public string Revision => _revisionBox.Text.Trim();
        public string GeneratedFileName => $"{AssemblyNumber}-BOM-Rev-{Revision}.xlsx";

        public BomDetailsDialog(string assemblyNumber, string revision)
        {
            Text = "Generate BoM | Document Details";
            FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            MinimizeBox = false;
            MaximizeBox = false;
            ShowInTaskbar = false;
            ClientSize = new System.Drawing.Size(480, 230);
            Font = new System.Drawing.Font("Segoe UI", 9F);

            var layout = new System.Windows.Forms.TableLayoutPanel
            {
                Dock = System.Windows.Forms.DockStyle.Fill,
                Padding = new System.Windows.Forms.Padding(16),
                ColumnCount = 2,
                RowCount = 5,
                AutoSize = false
            };
            layout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 145));
            layout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100));
            layout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 36));
            layout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 36));
            layout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 52));
            layout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100));
            layout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 42));

            _assemblyNumberBox.Text = assemblyNumber;
            _revisionBox.Text = revision;
            AddInputRow(layout, 0, "Assembly number", _assemblyNumberBox);
            AddInputRow(layout, 1, "BoM revision", _revisionBox);

            var previewPanel = new System.Windows.Forms.Panel
            {
                Dock = System.Windows.Forms.DockStyle.Fill,
                BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle,
                Padding = new System.Windows.Forms.Padding(8)
            };
            var previewTitle = new System.Windows.Forms.Label
            {
                Text = "FILE NAME",
                Dock = System.Windows.Forms.DockStyle.Top,
                Height = 18,
                ForeColor = System.Drawing.Color.DimGray
            };
            _fileNamePreview.Dock = System.Windows.Forms.DockStyle.Fill;
            _fileNamePreview.AutoEllipsis = true;
            _fileNamePreview.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            previewPanel.Controls.Add(_fileNamePreview);
            previewPanel.Controls.Add(previewTitle);
            layout.Controls.Add(previewPanel, 0, 2);
            layout.SetColumnSpan(previewPanel, 2);

            _validationMessage.Dock = System.Windows.Forms.DockStyle.Fill;
            _validationMessage.ForeColor = System.Drawing.Color.Firebrick;
            _validationMessage.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            layout.Controls.Add(_validationMessage, 0, 3);
            layout.SetColumnSpan(_validationMessage, 2);

            var buttons = new System.Windows.Forms.FlowLayoutPanel
            {
                Dock = System.Windows.Forms.DockStyle.Fill,
                FlowDirection = System.Windows.Forms.FlowDirection.RightToLeft,
                WrapContents = false
            };
            var generateButton = new System.Windows.Forms.Button
            {
                Text = "Continue",
                AutoSize = true,
                DialogResult = System.Windows.Forms.DialogResult.None
            };
            var cancelButton = new System.Windows.Forms.Button
            {
                Text = "Cancel",
                AutoSize = true,
                DialogResult = System.Windows.Forms.DialogResult.Cancel
            };
            generateButton.Click += (_, _) => ValidateAndAccept();
            buttons.Controls.Add(generateButton);
            buttons.Controls.Add(cancelButton);
            layout.Controls.Add(buttons, 0, 4);
            layout.SetColumnSpan(buttons, 2);

            Controls.Add(layout);
            AcceptButton = generateButton;
            CancelButton = cancelButton;

            _assemblyNumberBox.TextChanged += (_, _) => UpdatePreview();
            _revisionBox.TextChanged += (_, _) => UpdatePreview();
            UpdatePreview();
        }

        private static void AddInputRow(
            System.Windows.Forms.TableLayoutPanel layout,
            int row,
            string labelText,
            System.Windows.Forms.TextBox textBox)
        {
            var label = new System.Windows.Forms.Label
            {
                Text = labelText,
                Dock = System.Windows.Forms.DockStyle.Fill,
                TextAlign = System.Drawing.ContentAlignment.MiddleLeft
            };
            textBox.Dock = System.Windows.Forms.DockStyle.Fill;
            textBox.Margin = new System.Windows.Forms.Padding(3, 5, 3, 5);
            layout.Controls.Add(label, 0, row);
            layout.Controls.Add(textBox, 1, row);
        }

        private void UpdatePreview()
        {
            _fileNamePreview.Text = GeneratedFileName;
        }

        private void ValidateAndAccept()
        {
            if (string.IsNullOrWhiteSpace(AssemblyNumber) || string.IsNullOrWhiteSpace(Revision))
            {
                _validationMessage.Text = "Assembly number and BoM revision are both required.";
                return;
            }

            char[] invalidCharacters = System.IO.Path.GetInvalidFileNameChars();
            if (new[] { AssemblyNumber, Revision }.Any(value => value.IndexOfAny(invalidCharacters) >= 0))
            {
                _validationMessage.Text = "Assembly number and revision cannot contain filename characters such as \\, /, :, or *.";
                return;
            }

            _validationMessage.Text = string.Empty;
            DialogResult = System.Windows.Forms.DialogResult.OK;
            Close();
        }
    }
}
