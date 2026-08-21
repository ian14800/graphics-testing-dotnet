using Esri.ArcGISRuntime.Mapping;
using Esri.ArcGISRuntime.Portal;
using Esri.ArcGISRuntime.Symbology;
using Esri.ArcGISRuntime.Geometry;
using Esri.ArcGISRuntime.UI;
using System.Diagnostics;
using System.IO;
using System.Collections.Specialized;
using System.Globalization;
using System.Linq;
using System.Text;
using Esri.ArcGISRuntime.Data;
using System.Linq.Expressions;

namespace NETGraphicsTester
{
    public partial class MainPage : ContentPage
    {
        GraphicsOverlay graphicsOverlay = new GraphicsOverlay();
        SimpleRenderer redCircle = new SimpleRenderer(new SimpleMarkerSymbol(SimpleMarkerSymbolStyle.Circle, System.Drawing.Color.Red, 12));
        Stopwatch drawClock = new Stopwatch();
        int[] symbolTypeCounts = new int[3] { 0, 0, 0 };

        public MainPage()
        {
            InitializeComponent();
            graphicsOverlay.Graphics.CollectionChanged += OnGraphicsCollectionChanged;
            UpdateGraphicsCountLabel();
            _ = InitializeSceneAsync();
        }

        Random random = new Random();
        int currentRenderer = 0;
        int surfacePlacement = 0;
        double currentOpacity = 1.0;
        bool overlayVisibilty = true;
        bool isBatchRun = false;
        Graphic? currentlyIdentifiedGraphic;

        private const int ModelClassMin = 1;
        private const int ModelClassMax = 5;
        private string currentScene = "Blank";
        private bool initialSetup = true;

        private string GetCurrentScene()
        {
            Picker? scenePicker = this.FindByName<Picker>("ScenePicker");
            if (scenePicker == null || scenePicker.SelectedItem == null)
            {
                return "Blank";
            }

            return scenePicker.SelectedItem.ToString() ?? "Blank";
        }

        private int GetRequestedCount()
        {
            Entry? countEntry = this.FindByName<Entry>("CountEntry");
            if (!int.TryParse(countEntry?.Text, out int count) || count <= 0)
            {
                StatusLabel.Text = "Enter a positive whole number in Count.";
                return 0;
            }

            return count;
        }

        private int GetRunsCount()
        {
            Entry? countEntry = this.FindByName<Entry>("RunsCount");
            if (!int.TryParse(countEntry?.Text, out int count) || count <= 0)
            {
                StatusLabel.Text = "Enter a positive whole number in Count.";
                return 0;
            }

            return count;
        }

        private void SceneView_DrawStatusChanged(object? sender, EventArgs e)
        {
            if (isBatchRun || sceneView == null)
            {
                return;
            }

            if (sceneView.DrawStatus == DrawStatus.Completed)
            {
                drawClock.Stop();
                DrawTimer.Text = $"{drawClock.ElapsedMilliseconds} ms // draw timer";
            }
        }

        private void SceneView_WarningsChanged(object? sender, EventArgs e)
        {
            if (sender is Esri.ArcGISRuntime.Maui.LocalSceneView localSceneView)
            {
                LogSceneViewWarnings(localSceneView);
            }
        }

        private static void LogSceneViewWarnings(Esri.ArcGISRuntime.Maui.LocalSceneView localSceneView)
        {
            foreach (Exception warning in localSceneView.Warnings)
            {
                Debug.WriteLine($"LocalSceneView warning: {warning}");
            }
        }

        private async Task InitializeSceneAsync()
        {
            currentScene = GetCurrentScene();

            if (currentScene == "Blank")
            {
                try
                {
                    var localSceneView = sceneView;
                    if (localSceneView == null)
                    {
                        StatusLabel.Text = "Scene view is not ready.";
                        return;
                    }

                    localSceneView.WarningsChanged += SceneView_WarningsChanged;

                    var scene = new Scene(SceneViewingMode.Local, BasemapStyle.ArcGISTopographic);
                    var camera = new Camera(37.7, -122.4194, 15000, 0, 30, 0);

                    await scene.LoadAsync();

                    scene.BaseSurface.NavigationConstraint = NavigationConstraint.None;

                    localSceneView.Scene = scene;
                    
                    if (initialSetup)
                    {
                        graphicsOverlay.Renderer = redCircle;
                        graphicsOverlay.SceneProperties.SurfacePlacement = SurfacePlacement.Absolute;
                        graphicsOverlay.SceneProperties.AltitudeOffset = 0;
                        var overlays = localSceneView.GraphicsOverlays;
                        if (overlays == null)
                        {
                            StatusLabel.Text = "Graphics overlay collection is unavailable.";
                            return;
                        }

                        overlays.Add(graphicsOverlay);
                        initialSetup = false;
                    }

                    localSceneView.SetViewpointCamera(camera);
                    localSceneView.DrawStatusChanged += SceneView_DrawStatusChanged;
                    localSceneView.GeoViewTapped += OnSceneViewTapped;
                    LogSceneViewWarnings(localSceneView);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error thrown: {ex.Message}");
                }
            } else
            {
                string sceneId = "";
                switch(currentScene)
                {
                    case "Heavy":
                        sceneId = "5e43584386f54533b7c30087d1051343";
                        break;
                    case "Medium":
                        sceneId = "e10369820fa04866a16338cce14c0f36";
                        break;
                    case "Light":
                        sceneId = "3023142800a64ea1b6ec3459c05c9ac0";
                        break;
                    default:
                        sceneId = "3023142800a64ea1b6ec3459c05c9ac0";
                        break;
                }

                try
                {
                    ArcGISPortal portal = await ArcGISPortal.CreateAsync();

                    PortalItem sceneItem = await PortalItem.CreateAsync(portal, sceneId);

                    Scene scene = new Scene(sceneItem);

                    sceneView.Scene = scene;
                } catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error loading scene: {ex.Message}");
                }
            }  
        }

        private void OnScenePickerSelectionChanged(object sender, EventArgs e)
        {
            string selectedScene = GetCurrentScene();
            if (selectedScene != currentScene)
            {
                currentScene = selectedScene;
                _ = InitializeSceneAsync();
            }
        }

        private void OnGraphicsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            UpdateGraphicsCountLabel();
        }

        private void UpdateGraphicsCountLabel()
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                Label? graphicsCountLabel = this.FindByName<Label>("GraphicsCountLabel");
                if (graphicsCountLabel != null)
                {
                    graphicsCountLabel.Text = $"Graphics: {graphicsOverlay.Graphics.Count}";
                }
            });
        }

        private void OnAddGraphicsClicked(object sender, EventArgs e)
        {
            AddGraphicsOperation(updateUi: true);
        }

        private void OnToggleManualOperationsClicked(object sender, EventArgs e)
        {
            ManualOperationsPanel.IsVisible = !ManualOperationsPanel.IsVisible;
            ToggleManualOperationsButton.Text = ManualOperationsPanel.IsVisible
                ? "Hide controls"
                : "Show controls";
        }

        private void AddGraphicsOperation(bool updateUi)
        {
            int count = GetRequestedCount();
            if (count <= 0)
            {
                return;
            }

            Envelope? extent = sceneView.GetCurrentViewpoint(ViewpointType.BoundingGeometry)?.TargetGeometry as Envelope;
            if (extent == null)
            {
                System.Diagnostics.Debug.WriteLine("Could not determine current viewpoint extent.");
                StatusLabel.Text = "Could not determine current viewpoint extent.";
                return;
            }

            MapPoint? center = sceneView.GetCurrentViewpoint(ViewpointType.CenterAndScale)?.TargetGeometry as MapPoint;
            double baseZ = center?.Z ?? 0;
            const double zRange = 500;

            if (updateUi)
            {
                EventTimer.Text = " // event time";
                DrawTimer.Text = " // draw time";
            }
            Stopwatch operationTimer = Stopwatch.StartNew();
            if (updateUi)
            {
                drawClock.Reset();
                drawClock.Start();
            }


            for (int i = 0; i < count; i++)
            {
                double x = extent.XMin + (random.NextDouble() * (extent.XMax - extent.XMin));
                double y = extent.YMin + (random.NextDouble() * (extent.YMax - extent.YMin));
                double z = baseZ + ((random.NextDouble() * 2 * zRange) - zRange);
                MapPoint point = new MapPoint(x, y, z, extent.SpatialReference);

                Graphic graphic = new Graphic(point);
                graphic.Attributes["class_value"] = random.Next(ModelClassMin, ModelClassMax + 1).ToString();
                graphic.Attributes["size_value"] = random.Next(0, 100 + 1).ToString();
                graphic.Attributes["transparency_value"] = random.Next(0, 100 + 1).ToString();
                graphic.Attributes["rotation_value"] = random.Next(0, 360 + 1).ToString();
                graphic.Attributes["color_value"] = random.Next(0, 100 + 1).ToString();
                graphicsOverlay.Graphics.Add(graphic);
                System.Diagnostics.Debug.WriteLine("Added graphic");
            }

            operationTimer.Stop();
            if (updateUi)
            {
                EventTimer.Text = $"{operationTimer.ElapsedMilliseconds} ms // event time";
                StatusLabel.Text = $"Added {count} graphics.";
            }
        }

        private void OnRemoveGraphicsClicked(object sender, EventArgs e)
        {
            RemoveGraphicsOperation(updateUi: true);
        }

        private void RemoveGraphicsOperation(bool updateUi)
        {
            int count = GetRequestedCount();
            if (count <= 0)
            {
                return;
            }

            int removeCount = Math.Min(count, graphicsOverlay.Graphics.Count);

            if (updateUi)
            {
                EventTimer.Text = " // event time";
                DrawTimer.Text = " // draw time";
            }
            Stopwatch operationTimer = Stopwatch.StartNew();
            if (updateUi)
            {
                drawClock.Reset();
                drawClock.Start();
            }


            for (int i = 0; i < removeCount; i++)
            {
                graphicsOverlay.Graphics.RemoveAt(graphicsOverlay.Graphics.Count - 1);
            }

            operationTimer.Stop();
            if (updateUi)
            {
                EventTimer.Text = $"{operationTimer.ElapsedMilliseconds} ms // event time";
                StatusLabel.Text = $"Removed {removeCount} graphics.";
            }
        }

        private async void OnSwapRendererClicked(object sender, EventArgs e)
        {
            await SwapRendererOperationAsync(updateUi: true);
        }

        private async Task SwapRendererOperationAsync(bool updateUi)
        {
            currentRenderer++;
            if (currentRenderer >= 5)
            {
                currentRenderer = 0;
            }

            if (updateUi)
            {
                EventTimer.Text = " // event time";
                DrawTimer.Text = " // draw time";
            }
            Stopwatch operationTimer = Stopwatch.StartNew();
            if (updateUi)
            {
                drawClock.Reset();
                drawClock.Start();
            }


            switch (currentRenderer)
            {
                case 0:
                    graphicsOverlay.Renderer = redCircle;
                    operationTimer.Stop();
                    if (updateUi)
                    {
                        EventTimer.Text = $"{operationTimer.ElapsedMilliseconds} ms // event time";
                        StatusLabel.Text = $"Renderer set to simple red circle.";
                    }
                    break;
                case 1:
                    SimpleRenderer blueSquare = new SimpleRenderer(new SimpleMarkerSymbol(SimpleMarkerSymbolStyle.Square, System.Drawing.Color.Blue, 18));
                    graphicsOverlay.Renderer = blueSquare;
                    operationTimer.Stop();
                    if (updateUi)
                    {
                        EventTimer.Text = $"{operationTimer.ElapsedMilliseconds} ms // event time";
                        StatusLabel.Text = $"Renderer set to simple blue square.";
                    }
                    break;
                case 2:
                    try
                    {
                        UniqueValueRenderer uniqueValueModels = new UniqueValueRenderer();
                        uniqueValueModels.FieldNames.Add("class_value");

                        MultilayerPointSymbol modelSymbol1 = await createModelLayerFromFile("1");
                        uniqueValueModels.UniqueValues.Add(new UniqueValue("Model 1", "1.glb", modelSymbol1, "1"));

                        MultilayerPointSymbol modelSymbol2 = await createModelLayerFromFile("2");
                        uniqueValueModels.UniqueValues.Add(new UniqueValue("Model 2", "2.glb", modelSymbol2, "2"));

                        MultilayerPointSymbol modelSymbol3 = await createModelLayerFromFile("3");
                        uniqueValueModels.UniqueValues.Add(new UniqueValue("Model 3", "3.glb", modelSymbol3, "3"));

                        MultilayerPointSymbol modelSymbol4 = await createModelLayerFromFile("4");
                        uniqueValueModels.UniqueValues.Add(new UniqueValue("Model 4", "4.glb", modelSymbol4, "4"));

                        MultilayerPointSymbol modelSymbol5 = await createModelLayerFromFile("5");
                        uniqueValueModels.UniqueValues.Add(new UniqueValue("Model 5", "5.glb", modelSymbol5, "5"));

                        uniqueValueModels.DefaultSymbol = modelSymbol1;
                        uniqueValueModels.DefaultLabel = "Default model";

                        graphicsOverlay.Renderer = uniqueValueModels;
                        operationTimer.Stop();
                        if (updateUi)
                        {
                            EventTimer.Text = $"{operationTimer.ElapsedMilliseconds} ms // event time";
                            StatusLabel.Text = $"Renderer set to 3D model unique values.";
                        }
                    }
                    catch (Exception ex)
                    {
                        operationTimer.Stop();
                        if (updateUi)
                        {
                            EventTimer.Text = $"{operationTimer.ElapsedMilliseconds} ms // event time";
                            StatusLabel.Text = $"Model renderer failed: {ex.Message}";
                        }
                    }
                    break;
                case 3:
                    try
                    {
                        UniqueValueRenderer uniqueValuePictures = new UniqueValueRenderer();
                        uniqueValuePictures.FieldNames.Add("class_value");

                        MultilayerPointSymbol pictureSymbol1 = await createPictureLayerFromFile("1");
                        uniqueValuePictures.UniqueValues.Add(new UniqueValue("Picture 1", "1.png", pictureSymbol1, "1"));

                        MultilayerPointSymbol pictureSymbol2 = await createPictureLayerFromFile("2");
                        uniqueValuePictures.UniqueValues.Add(new UniqueValue("Picture 2", "2.png", pictureSymbol2, "2"));

                        MultilayerPointSymbol pictureSymbol3 = await createPictureLayerFromFile("3");
                        uniqueValuePictures.UniqueValues.Add(new UniqueValue("Picture 3", "3.png", pictureSymbol3, "3"));

                        MultilayerPointSymbol pictureSymbol4 = await createPictureLayerFromFile("4");
                        uniqueValuePictures.UniqueValues.Add(new UniqueValue("Picture 4", "4.png", pictureSymbol4, "4"));

                        MultilayerPointSymbol pictureSymbol5 = await createPictureLayerFromFile("5");
                        uniqueValuePictures.UniqueValues.Add(new UniqueValue("Picture 5", "5.png", pictureSymbol5, "5"));


                        uniqueValuePictures.DefaultSymbol = pictureSymbol1;
                        uniqueValuePictures.DefaultLabel = "Default Picture";

                        graphicsOverlay.Renderer = uniqueValuePictures;
                        operationTimer.Stop();
                        if (updateUi)
                        {
                            EventTimer.Text = $"{operationTimer.ElapsedMilliseconds} ms // event time";
                            StatusLabel.Text = $"Renderer set to picture unique values.";
                        }
                    }
                    catch (Exception ex)
                    {
                        operationTimer.Stop();
                        if (updateUi)
                        {
                            EventTimer.Text = $"{operationTimer.ElapsedMilliseconds} ms // event time";
                            StatusLabel.Text = $"Picture renderer failed: {ex.Message}";
                        }
                    }
                    break;
                case 4:
                    SimpleMarkerSceneSymbol greenConeSymbol = new SimpleMarkerSceneSymbol(
                        SimpleMarkerSceneSymbolStyle.Cone,
                        System.Drawing.Color.ForestGreen,
                        50,
                        50,
                        80,
                        SceneSymbolAnchorPosition.Bottom);
                    SimpleRenderer visualVariablesRenderer = new SimpleRenderer(greenConeSymbol);

                    graphicsOverlay.Renderer = visualVariablesRenderer;
                    operationTimer.Stop();
                    if (updateUi)
                    {
                        EventTimer.Text = $"{operationTimer.ElapsedMilliseconds} ms // event time";
                        StatusLabel.Text = $"Renderer set to visual variables.";
                    }
                    break;
                default:
                    graphicsOverlay.Renderer = redCircle;
                    operationTimer.Stop();
                    if (updateUi)
                    {
                        EventTimer.Text = $"{operationTimer.ElapsedMilliseconds} ms // event time";
                        StatusLabel.Text = $"Renderer set to simple red circle.";
                    }
                    break;
            }
        }

        private async void OnSwapPlacementClicked(object sender, EventArgs e)
        {
            await SwapPlacementOperationAsync(updateUi: true);
        }

        private async Task SwapPlacementOperationAsync(bool updateUi)
        {
            surfacePlacement++;
            if (surfacePlacement > 4)
            {
                surfacePlacement = 0;
            }

            switch (surfacePlacement)
            {
                case 0:
                    graphicsOverlay.SceneProperties.SurfacePlacement = SurfacePlacement.Absolute;
                    if (updateUi)
                    {
                        StatusLabel.Text = "Overlay's surface placement updated to absolute.";
                    }
                    break;
                case 1:
                    graphicsOverlay.SceneProperties.SurfacePlacement = SurfacePlacement.Relative;
                    if (updateUi)
                    {
                        StatusLabel.Text = "Overlay's surface placement updated to relative.";
                    }
                    break;
                case 2:
                    graphicsOverlay.SceneProperties.SurfacePlacement = SurfacePlacement.DrapedBillboarded;
                    if (updateUi)
                    {
                        StatusLabel.Text = "Overlay's surface placement updated to draped billboarded.";
                    }
                    break;
                case 3:
                    graphicsOverlay.SceneProperties.SurfacePlacement = SurfacePlacement.DrapedFlat;
                    if (updateUi)
                    {
                        StatusLabel.Text = "Overlay's surface placement updated to draped flat.";
                    }
                    break;
       
                case 4:
                    graphicsOverlay.SceneProperties.SurfacePlacement = SurfacePlacement.RelativeToScene;
                    if (updateUi)
                    {
                        StatusLabel.Text = "Overlay's surface placement updated to relative to scene.";
                    }
                    break;
                default:
                    graphicsOverlay.SceneProperties.SurfacePlacement = SurfacePlacement.Absolute;
                    if (updateUi)
                    {
                        StatusLabel.Text = "Overlay's surface placement updated to absolute.";
                    }
                    break;

                    
            }
        }

        private async void OnAddAltitudeOffsetClicked(object sender, EventArgs e)
        {
            await AddAltitudeOffsetOperationAsync(updateUi: true);
        }

        private async Task AddAltitudeOffsetOperationAsync(bool updateUi)
        {
            graphicsOverlay.SceneProperties.AltitudeOffset += 10000;
            if (updateUi)
            {
                StatusLabel.Text = $"Altitude offset increased to: {graphicsOverlay.SceneProperties.AltitudeOffset}";
            }
        }

        async private Task<MultilayerPointSymbol> createModelLayerFromFile(string fieldValue, int size = 50)
        {
            string file = $"{fieldValue}.glb";

            string filePath = Path.Combine(FileSystem.CacheDirectory, file);
            if (!File.Exists(filePath))
            {
                using var sourceStream = await FileSystem.OpenAppPackageFileAsync(file);
                using var destStream = File.Create(filePath);
                await sourceStream.CopyToAsync(destStream);
            }
            ModelSymbolLayer modelLayer = new ModelSymbolLayer(new Uri(filePath, UriKind.Absolute)) { Height = size, Width = size, Depth = size };
            await modelLayer.LoadAsync();
            MultilayerPointSymbol multilayerSymbol = new MultilayerPointSymbol(new SymbolLayer[] { modelLayer });
            return multilayerSymbol;
        }

        async private Task<MultilayerPointSymbol> createPictureLayerFromFile(string fieldValue, double size = 80.0)
        {
            string file = $"{fieldValue}.png";

            string picturePath = Path.Combine(FileSystem.CacheDirectory, file);
            if (!File.Exists(picturePath))
            {
                using var sourceStream = await FileSystem.OpenAppPackageFileAsync(file);
                using var destStream = File.Create(picturePath);
                await sourceStream.CopyToAsync(destStream);
            }
            PictureMarkerSymbolLayer pictureLayer = new PictureMarkerSymbolLayer(new Uri(picturePath, UriKind.Absolute)) { Size = size };
            MultilayerPointSymbol multilayerSymbol = new MultilayerPointSymbol(new SymbolLayer[] { pictureLayer });
            return multilayerSymbol;
        }

        private void OnMoveGraphicsUniformClicked(object sender, EventArgs e)
        {
            MoveGraphicsUniformOperation(updateUi: true);
        }

        private void MoveGraphicsUniformOperation(bool updateUi)
        {
            int count = GetRequestedCount();
            if (count <= 0)
            {
                return;
            }

            double dx = random.Next(-1000, 1000);
            double dy = random.Next(-1000, 1000);
            double dz = random.Next(-1000, 1000);

            int graphicsToMove = Math.Min(count, graphicsOverlay.Graphics.Count);

            if (updateUi)
            {
                EventTimer.Text = " // event time";
                DrawTimer.Text = " // draw time";
            }
            Stopwatch operationTimer = Stopwatch.StartNew();
            if (updateUi)
            {
                drawClock.Reset();
                drawClock.Start();
            }

            for (int i = 0; i < graphicsToMove; i++)
            {
                Graphic graphic = graphicsOverlay.Graphics[i];

                if (graphic.Geometry is MapPoint currentPoint)
                {
                    MapPoint movedPoint = new MapPoint(
                        currentPoint.X + dx,
                        currentPoint.Y + dy,
                        currentPoint.Z + dz,
                        currentPoint.SpatialReference);

                    graphic.Geometry = movedPoint;
                }
            }

            operationTimer.Stop();
            if (updateUi)
            {
                EventTimer.Text = $"{operationTimer.ElapsedMilliseconds} ms // event time";
                StatusLabel.Text = $"Moved {graphicsToMove} graphics by ({dx:0}, {dy:0}, {dz:0}).";
            }
        }

        private void OnMoveGraphicsRandomClicked(object sender, EventArgs e)
        {
            MoveGraphicsRandomOperation(updateUi: true);
        }

        private void MoveGraphicsRandomOperation(bool updateUi)
        {
            int count = GetRequestedCount();
            if (count <= 0)
            {
                return;
            }

            int graphicsToMove = Math.Min(count, graphicsOverlay.Graphics.Count);

            if (updateUi)
            {
                EventTimer.Text = " // event time";
                DrawTimer.Text = " // draw time";
            }
            Stopwatch operationTimer = Stopwatch.StartNew();
            if (updateUi)
            {
                drawClock.Reset();
                drawClock.Start();
            }


            for (int i = 0; i < graphicsToMove; i++)
            {
                double dx = random.Next(-100, 100);
                double dy = random.Next(-100, 100);
                double dz = random.Next(-100, 100);
                Graphic graphic = graphicsOverlay.Graphics[i];

                if (graphic.Geometry is MapPoint currentPoint)
                {
                    MapPoint movedPoint = new MapPoint(
                        currentPoint.X + dx,
                        currentPoint.Y + dy,
                        currentPoint.Z + dz,
                        currentPoint.SpatialReference);

                    graphic.Geometry = movedPoint;
                }
            }

            operationTimer.Stop();
            if (updateUi)
            {
                EventTimer.Text = $"{operationTimer.ElapsedMilliseconds} ms // event time";
                StatusLabel.Text = $"Moved {graphicsToMove} graphics randomly.";
            }
        }

        async private void OnAddSymbolsClicked(object sender, EventArgs e)
        {
            await AddSymbolsOperationAsync(updateUi: true);
        }

        private async Task AddSymbolsOperationAsync(bool updateUi)
        {
            int count = GetRequestedCount();
            if (count <= 0)
            {
                return;
            }

            int assignCount = Math.Min(count, graphicsOverlay.Graphics.Count);

            if (updateUi)
            {
                EventTimer.Text = " // event time";
                DrawTimer.Text = " // draw time";
            }
            Stopwatch operationTimer = Stopwatch.StartNew();
            if (updateUi)
            {
                drawClock.Reset();
                drawClock.Start();
            }

            SimpleMarkerSymbol greenTriangle = new SimpleMarkerSymbol(SimpleMarkerSymbolStyle.Triangle, System.Drawing.Color.Green, 40);
            MultilayerPointSymbol modelSymbol1 = await createModelLayerFromFile("1", 100);
            MultilayerPointSymbol modelSymbol2 = await createModelLayerFromFile("2", 100);
            MultilayerPointSymbol modelSymbol3 = await createModelLayerFromFile("3", 100);
            MultilayerPointSymbol modelSymbol4 = await createModelLayerFromFile("4", 100);
            MultilayerPointSymbol modelSymbol5 = await createModelLayerFromFile("5", 100);
            MultilayerPointSymbol pictureSymbol1 = await createPictureLayerFromFile("1", 100);
            MultilayerPointSymbol pictureSymbol2 = await createPictureLayerFromFile("2", 100);
            MultilayerPointSymbol pictureSymbol3 = await createPictureLayerFromFile("3", 100);
            MultilayerPointSymbol pictureSymbol4 = await createPictureLayerFromFile("4", 100);
            MultilayerPointSymbol pictureSymbol5 = await createPictureLayerFromFile("5", 100);
            for (int i = 0; i < assignCount; i++)
            {
                int symbolType = random.Next(1, 4);
                string symbolIndex = random.Next(5, 11).ToString();
                Graphic graphic = graphicsOverlay.Graphics[i];

                switch (symbolType)
                {
                    case 1:
                        graphic.Symbol = greenTriangle;
                        symbolTypeCounts[0]++;
                        break;
                    case 2:
                        MultilayerPointSymbol modelSymbol = symbolIndex switch
                        {
                            "5" => modelSymbol5,
                            "6" => modelSymbol1,
                            "7" => modelSymbol2,
                            "8" => modelSymbol3,
                            "9" => modelSymbol4,
                            "10" => modelSymbol5,
                            _ => modelSymbol1
                        };
                        graphic.Symbol = modelSymbol;
                        symbolTypeCounts[1]++;
                        break;
                    case 3:
                        MultilayerPointSymbol pictureSymbol = symbolIndex switch
                        {
                            "5" => pictureSymbol5,
                            "6" => pictureSymbol1,
                            "7" => pictureSymbol2,
                            "8" => pictureSymbol3,
                            "9" => pictureSymbol4,
                            "10" => pictureSymbol5,
                            _ => pictureSymbol1
                        };
                        graphic.Symbol = pictureSymbol;
                        symbolTypeCounts[2]++;
                        break;
                    default:
                        graphic.Symbol = greenTriangle;
                        break;
                    
                }

            }

            operationTimer.Stop();
            if (updateUi)
            {
                EventTimer.Text = $"{operationTimer.ElapsedMilliseconds} ms // event time";
                StatusLabel.Text = $"Assigned symbols to {assignCount} graphics. Symbol type ratio: {symbolTypeCounts[0]}S / {symbolTypeCounts[1]} M / {symbolTypeCounts[2]} P";
                symbolTypeCounts[0] = 0;
                symbolTypeCounts[1] = 0;
                symbolTypeCounts[2] = 0;
            }
        }

        private void OnRemoveOverlayClicked(object sender, EventArgs e)
        {
            RemoveOverlayOperation(updateUi: true);
        }

        private void RemoveOverlayOperation(bool updateUi)
        {
            var localSceneView = sceneView;
            if (localSceneView == null)
            {
                return;
            }

            var overlays = localSceneView.GraphicsOverlays;
            if (overlays == null)
            {
                return;
            }

            overlays.Remove(graphicsOverlay);

            if (updateUi)
            {
                StatusLabel.Text = $"Removed overlay.";
            }
        }
        private void OnAddOverlayClicked(object sender, EventArgs e)
        {
            AddOverlayOperation(updateUi: true);
        }

        private void AddOverlayOperation(bool updateUi)
        {
            var localSceneView = sceneView;
            if (localSceneView == null)
            {
                return;
            }

            var overlays = localSceneView.GraphicsOverlays;
            if (overlays == null)
            {
                return;
            }

            overlays.Add(graphicsOverlay);

            if (updateUi)
            {
                StatusLabel.Text = $"Added overlay.";
            }
        }

        private void OnToggleVisibiltyClicked(object sender, EventArgs e)
        {
            ToggleVisibilityOperation(updateUi: true);
        }

        private void ToggleVisibilityOperation(bool updateUi)
        {
            overlayVisibilty = !overlayVisibilty;

            graphicsOverlay.IsVisible = overlayVisibilty;

            if (updateUi)
            {
                StatusLabel.Text = $"Overlay visibilty set to {overlayVisibilty}";
            }
        }

        private void OnToggleOpacityClicked(object sender, EventArgs e)
        {
            ToggleOpacityOperation(updateUi: true);
        }

        private void ToggleOpacityOperation(bool updateUi)
        {
            if (currentOpacity == 0.0)
            {
                currentOpacity = 1.0;
            } else
            {
                currentOpacity -= .25;
            }
      
            graphicsOverlay.Opacity = currentOpacity;

            if (updateUi)
            {
                StatusLabel.Text = $"Overlay opacity is set to {currentOpacity}";
            }
        }

        private void OnToggleGraphicsVisibilityClicked(object sender, EventArgs e)
        {
            ToggleGraphicsVisibilityOperation(updateUi: true);
        }

        private void ToggleGraphicsVisibilityOperation(bool updateUi)
        {
            int count = GetRequestedCount();
            if (count <= 0)
            {
                return;
            }

            int removeCount = Math.Min(count, graphicsOverlay.Graphics.Count);

            if (updateUi)
            {
                EventTimer.Text = " // event time";
                DrawTimer.Text = " // draw time";
            }
            Stopwatch operationTimer = Stopwatch.StartNew();
            if (updateUi)
            {
                drawClock.Reset();
                drawClock.Start();
            }


            for (int i = 0; i < removeCount; i++)
            {
                if (graphicsOverlay.Graphics[i].IsVisible)
                {
                    graphicsOverlay.Graphics[i].IsVisible = false;
                } else
                {
                    graphicsOverlay.Graphics[i].IsVisible = true;
                }
            }

            operationTimer.Stop();
            if (updateUi)
            {
                EventTimer.Text = $"{operationTimer.ElapsedMilliseconds} ms // event time";
                StatusLabel.Text = $"Toggled visibility for {removeCount} graphics.";
            }
        }

        private void OnToggleGraphicsSelectedClicked(object sender, EventArgs e)
        {
            ToggleGraphicsSelectedOperation(updateUi: true);
        }

        private void ToggleGraphicsSelectedOperation(bool updateUi)
        {
            int count = GetRequestedCount();
            if (count <= 0)
            {
                return;
            }

            int removeCount = Math.Min(count, graphicsOverlay.Graphics.Count);

            if (updateUi)
            {
                EventTimer.Text = " // event time";
                DrawTimer.Text = " // draw time";
            }
            Stopwatch operationTimer = Stopwatch.StartNew();
            if (updateUi)
            {
                drawClock.Reset();
                drawClock.Start();
            }


            for (int i = 0; i < removeCount; i++)
            {
                if (graphicsOverlay.Graphics[i].IsSelected)
                {
                    graphicsOverlay.Graphics[i].IsSelected = false;
                }
                else
                {
                    graphicsOverlay.Graphics[i].IsSelected = true;
                }
            }

            operationTimer.Stop();
            if (updateUi)
            {
                EventTimer.Text = $"{operationTimer.ElapsedMilliseconds} ms // event time";
                StatusLabel.Text = $"Toggled selection for {removeCount} graphics.";
            }
        }

        //private async Task<string> SaveRunTimesCsvAsync(string workflow, double[] runTimes)
        //{
        //    string safeWorkflow = string.Join("_", workflow.Split(Path.GetInvalidFileNameChars()));
        //    if (string.IsNullOrWhiteSpace(safeWorkflow))
        //    {
        //        safeWorkflow = "workflow";
        //    }

        //    string timestamp = DateTime.Now.ToString("yyyyMMdd-HHmmss", CultureInfo.InvariantCulture);
        //    string fileName = $"run-times-{safeWorkflow}-{timestamp}.csv";
        //    string filePath = Path.Combine(FileSystem.Current.AppDataDirectory, fileName);

        //    StringBuilder csvBuilder = new StringBuilder();
        //    csvBuilder.AppendLine("run,time_ms");

        //    for (int i = 0; i < runTimes.Length; i++)
        //    {
        //        csvBuilder.Append(i + 1);
        //        csvBuilder.Append(',');
        //        csvBuilder.AppendLine(runTimes[i].ToString("0.###", CultureInfo.InvariantCulture));
        //    }

        //    await File.WriteAllTextAsync(filePath, csvBuilder.ToString());
        //    return filePath;
        //}

        private async void OnRunAnimationsClicked(object sender, EventArgs e)
        {
            int runsCount = GetRunsCount();
            int count = GetRequestedCount();
            string workflow = WorkflowPicker.SelectedItem?.ToString() ?? "None";

            if (count <= 0 || runsCount <= 0)
            {
                return;
            }

            EventTimer.Text = " // event time";
            DrawTimer.Text = " // draw time";
            Stopwatch operationTimer = Stopwatch.StartNew();
            Stopwatch runTimer = new Stopwatch();

            double[] runTimes = new double[runsCount];
            isBatchRun = true;

            try
            {
                for (int i = 0; i < runsCount; i++)
                {
                    runTimer.Restart();
                    switch (workflow)
                    {
                        case "Move graphics (uniform)":
                            MoveGraphicsUniformOperation(updateUi: false);
                            break;
                        case "Move graphics (random)":
                            MoveGraphicsRandomOperation(updateUi: false);
                            break;
                        case "Swap renderer":
                            await SwapRendererOperationAsync(updateUi: false);
                            break;
                        case "Add symbols":
                            await AddSymbolsOperationAsync(updateUi: false);
                            break;
                        case "Add/remove overlay":
                            RemoveOverlayOperation(updateUi: false);
                            AddOverlayOperation(updateUi: false);
                            break;
                        case "Add/move/remove":
                            AddGraphicsOperation(updateUi: false);
                            MoveGraphicsRandomOperation(updateUi: false);
                            RemoveGraphicsOperation(updateUi: false);
                            break;
                        case "Add/give symbols/remove":
                            AddGraphicsOperation(updateUi: false);
                            await AddSymbolsOperationAsync(updateUi: false);
                            RemoveGraphicsOperation(updateUi: false);
                            break;
                        case "Add/give symbols/remove overlay":
                            AddGraphicsOperation(updateUi: false);
                            await AddSymbolsOperationAsync(updateUi: false);
                            RemoveOverlayOperation(updateUi: false);
                            RemoveGraphicsOperation(updateUi: false);
                            AddOverlayOperation(updateUi: false);
                            break;
                        default:
                            StatusLabel.Text = $"Unknown workflow: {workflow}";
                            return;
                    }
                    runTimer.Stop();
                    runTimes[i] = runTimer.Elapsed.TotalMilliseconds;
                }
            }
            finally
            {
                isBatchRun = false;
            }

            operationTimer.Stop();
            EventTimer.Text = $"Total time: {operationTimer.ElapsedMilliseconds} ms // slowest time: {runTimes.Max()} // fastest time: {runTimes.Min()} // average time: {runTimes.Average()}";

            try
            {
                //string csvPath = await SaveRunTimesCsvAsync(workflow, runTimes);
                StatusLabel.Text = $"Completed {runsCount} runs of workflow: {workflow}.";
            }
            catch (Exception ex)
            {
                StatusLabel.Text = $"Completed {runsCount} runs of workflow: {workflow}. CSV save failed: {ex.Message}";
            }
        }

        private async void OnSceneViewTapped(object? sender, Esri.ArcGISRuntime.Maui.GeoViewInputEventArgs e)
        {
            double tolerance = 20.0;
            int maximumResults = 1;
            bool onlyReturnPopups = false;

            try
            {
                IdentifyGraphicsOverlayResult identifyResult = await sceneView.IdentifyGraphicsOverlayAsync(
                    graphicsOverlay,
                    e.Position,
                    tolerance,
                    onlyReturnPopups,
                    maximumResults);

                if (identifyResult.Graphics.Count > 0)
                {
                    Graphic identifiedGraphic = identifyResult.Graphics.First();

                    string class_value = GetAttributeString(identifiedGraphic, "class_value");
                    string size_value = GetAttributeString(identifiedGraphic, "size_value");
                    string transparency_value = GetAttributeString(identifiedGraphic, "transparency_value");
                    string rotation_value = GetAttributeString(identifiedGraphic, "rotation_value");
                    string color_value = GetAttributeString(identifiedGraphic, "color_value");

                    StatusLabel.Text = $"Identified graphic: class_value: {class_value}, {size_value}, {transparency_value}, {rotation_value}, {color_value}";
                    ShowIdentifyEditorForGraphic(identifiedGraphic);
                }
                else
                {
                    StatusLabel.Text = "Zero graphics identified.";
                    IdentifyPopupOverlay.IsVisible = false;
                }
            }
            catch (Exception ex)
            {
                StatusLabel.Text = $"Error identifying graphics: {ex.Message}";
                IdentifyPopupOverlay.IsVisible = false;
            }
        }

        private static string GetAttributeString(Graphic graphic, string attributeKey)
        {
            if (graphic.Attributes.TryGetValue(attributeKey, out object? value) && value != null)
            {
                return value.ToString() ?? string.Empty;
            }

            return string.Empty;
        }

        private void ShowIdentifyEditorForGraphic(Graphic graphic)
        {
            currentlyIdentifiedGraphic = graphic;
            ClassValueEntry.Text = GetAttributeString(graphic, "class_value");
            SizeValueEntry.Text = GetAttributeString(graphic, "size_value");
            TransparencyValueEntry.Text = GetAttributeString(graphic, "transparency_value");
            RotationValueEntry.Text = GetAttributeString(graphic, "rotation_value");
            ColorValueEntry.Text = GetAttributeString(graphic, "color_value");
            IdentifyPopupOverlay.IsVisible = true;
        }

        private void OnIdentifyEditorCancelClicked(object sender, EventArgs e)
        {
            currentlyIdentifiedGraphic = null;
            IdentifyPopupOverlay.IsVisible = false;
        }

        private void OnIdentifyEditorOkClicked(object sender, EventArgs e)
        {
            if (currentlyIdentifiedGraphic == null)
            {
                IdentifyPopupOverlay.IsVisible = false;
                return;
            }

            currentlyIdentifiedGraphic.Attributes["class_value"] = ClassValueEntry.Text ?? string.Empty;
            currentlyIdentifiedGraphic.Attributes["size_value"] = SizeValueEntry.Text ?? string.Empty;
            currentlyIdentifiedGraphic.Attributes["transparency_value"] = TransparencyValueEntry.Text ?? string.Empty;
            currentlyIdentifiedGraphic.Attributes["rotation_value"] = RotationValueEntry.Text ?? string.Empty;
            currentlyIdentifiedGraphic.Attributes["color_value"] = ColorValueEntry.Text ?? string.Empty;

            StatusLabel.Text = "Updated identified graphic attributes.";
            IdentifyPopupOverlay.IsVisible = false;
            currentlyIdentifiedGraphic = null;
        }

    }
}