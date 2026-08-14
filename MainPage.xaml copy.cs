using Esri.ArcGISRuntime.Mapping;
using Esri.ArcGISRuntime.Portal;
using Esri.ArcGISRuntime.Symbology;
using Esri.ArcGISRuntime.Geometry;
using Esri.ArcGISRuntime.UI;
using System.Diagnostics;
using System.IO;
using System.Collections.Specialized;
using System.Linq;

namespace NETGraphicsTester
{
    public partial class MainPage : ContentPage
    {
        GraphicsOverlay graphicsOverlay = new GraphicsOverlay();
        SimpleRenderer redCircle = new SimpleRenderer(new SimpleMarkerSymbol(SimpleMarkerSymbolStyle.Circle, System.Drawing.Color.Red, 12));
        Stopwatch drawClock = new Stopwatch();
        public MainPage()
        {
            InitializeComponent();
            graphicsOverlay.Graphics.CollectionChanged += OnGraphicsCollectionChanged;
            UpdateGraphicsCountLabel();
            _ = InitializeSceneAsync();
        }

        Random random = new Random();
        int currentRenderer = 0;

        private const int ModelClassMin = 1;
        private const int ModelClassMax = 5;

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

        private void SceneView_DrawStatusChanged(object sender, EventArgs e)
        {
            if (sceneView.DrawStatus == DrawStatus.Completed)
            {
                drawClock.Stop();
                DrawTimer.Text = $"{drawClock.ElapsedMilliseconds} ms // draw timer";
            }
        }

        private async Task InitializeSceneAsync()
        {
            try
            {
                var scene = new Scene(SceneViewingMode.Local, BasemapStyle.ArcGISTopographic);
                var camera = new Camera(37.7, -122.4194, 15000, 0, 30, 0);

                await scene.LoadAsync();

                scene.BaseSurface.NavigationConstraint = NavigationConstraint.None;

                sceneView.Scene = scene;
                graphicsOverlay.Renderer = redCircle;
                sceneView.GraphicsOverlays.Add(graphicsOverlay);
                sceneView.SetViewpointCamera(camera);
                sceneView.DrawStatusChanged += SceneView_DrawStatusChanged;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error thrown: {ex.Message}");
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

            EventTimer.Text = " // event time";
            DrawTimer.Text = " // draw time";
            Stopwatch operationTimer = Stopwatch.StartNew();
            drawClock.Reset();
            drawClock.Start();


            for (int i = 0; i < count; i++)
            {
                double x = extent.XMin + (random.NextDouble() * (extent.XMax - extent.XMin));
                double y = extent.YMin + (random.NextDouble() * (extent.YMax - extent.YMin));
                double z = baseZ + ((random.NextDouble() * 2 * zRange) - zRange);
                MapPoint point = new MapPoint(x, y, z, extent.SpatialReference);

                Graphic graphic = new Graphic(point);
                graphic.Attributes["class_value"] = random.Next(ModelClassMin, ModelClassMax + 1).ToString();
                graphicsOverlay.Graphics.Add(graphic);
                System.Diagnostics.Debug.WriteLine("Added graphic");
            }

            operationTimer.Stop();
            EventTimer.Text = $"{operationTimer.ElapsedMilliseconds} ms // event time";
            StatusLabel.Text = $"Added {count} graphics.";
        }

        private void OnRemoveGraphicsClicked(object sender, EventArgs e)
        {
            int count = GetRequestedCount();
            if (count <= 0)
            {
                return;
            }

            int removeCount = Math.Min(count, graphicsOverlay.Graphics.Count);

            EventTimer.Text = " // event time";
            DrawTimer.Text = " // draw time";
            Stopwatch operationTimer = Stopwatch.StartNew();
            drawClock.Reset();
            drawClock.Start();


            for (int i = 0; i < removeCount; i++)
            {
                graphicsOverlay.Graphics.RemoveAt(graphicsOverlay.Graphics.Count - 1);
            }

            operationTimer.Stop();
            EventTimer.Text = $"{operationTimer.ElapsedMilliseconds} ms // event time";
            StatusLabel.Text = $"Removed {removeCount} graphics.";
        }

        private async void OnSwapRendererClicked(object sender, EventArgs e)
        {
            currentRenderer++;
            if (currentRenderer >= 5)
            {
                currentRenderer = 0;
            }

            EventTimer.Text = " // event time";
            DrawTimer.Text = " // draw time";
            Stopwatch operationTimer = Stopwatch.StartNew();
            drawClock.Reset();
            drawClock.Start();


            switch (currentRenderer)
            {
                case 0:
                    graphicsOverlay.Renderer = redCircle;
                    operationTimer.Stop();
                    EventTimer.Text = $"{operationTimer.ElapsedMilliseconds} ms // event time";
                    StatusLabel.Text = $"Renderer set to simple red circle.";
                    break;
                case 1:
                    SimpleRenderer blueSquare = new SimpleRenderer(new SimpleMarkerSymbol(SimpleMarkerSymbolStyle.Square, System.Drawing.Color.Blue, 18));
                    graphicsOverlay.Renderer = blueSquare;
                    operationTimer.Stop();
                    EventTimer.Text = $"{operationTimer.ElapsedMilliseconds} ms // event time";
                    StatusLabel.Text = $"Renderer set to simple blue square.";
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
                        EventTimer.Text = $"{operationTimer.ElapsedMilliseconds} ms // event time";
                        StatusLabel.Text = $"Renderer set to 3D model unique values (class_value: 1-5).";
                    }
                    catch (Exception ex)
                    {
                        operationTimer.Stop();
                        EventTimer.Text = $"{operationTimer.ElapsedMilliseconds} ms // event time";
                        StatusLabel.Text = $"Model renderer failed: {ex.Message}";
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
                        EventTimer.Text = $"{operationTimer.ElapsedMilliseconds} ms // event time";
                        StatusLabel.Text = $"Renderer set to picture unique values (class_value: 1-5).";
                    }
                    catch (Exception ex)
                    {
                        operationTimer.Stop();
                        EventTimer.Text = $"{operationTimer.ElapsedMilliseconds} ms // event time";
                        StatusLabel.Text = $"Picture renderer failed: {ex.Message}";
                    }
                    break;
                case 4:
                    operationTimer.Stop();
                    EventTimer.Text = $"{operationTimer.ElapsedMilliseconds} ms // event time";
                    StatusLabel.Text = $"Renderer mode 4 is not configured yet.";
                    break;
                default:
                    graphicsOverlay.Renderer = redCircle;
                    operationTimer.Stop();
                    EventTimer.Text = $"{operationTimer.ElapsedMilliseconds} ms // event time";
                    StatusLabel.Text = $"Renderer set to simple red circle.";
                    break;
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
            int count = GetRequestedCount();
            if (count <= 0)
            {
                return;
            }

            double dx = random.Next(-1000, 1000);
            double dy = random.Next(-1000, 1000);
            double dz = random.Next(-1000, 1000);

            int graphicsToMove = Math.Min(count, graphicsOverlay.Graphics.Count);

            EventTimer.Text = " // event time";
            DrawTimer.Text = " // draw time";
            Stopwatch operationTimer = Stopwatch.StartNew();
            drawClock.Reset();
            drawClock.Start();

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
            EventTimer.Text = $"{operationTimer.ElapsedMilliseconds} ms // event time";
            StatusLabel.Text = $"Moved {graphicsToMove} graphics by ({dx:0}, {dy:0}, {dz:0}).";
        }

        private void OnMoveGraphicsRandomClicked(object sender, EventArgs e)
        {
            int count = GetRequestedCount();
            if (count <= 0)
            {
                return;
            }

            int graphicsToMove = Math.Min(count, graphicsOverlay.Graphics.Count);

            EventTimer.Text = " // event time";
            DrawTimer.Text = " // draw time";
            Stopwatch operationTimer = Stopwatch.StartNew();
            drawClock.Reset();
            drawClock.Start();


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
            EventTimer.Text = $"{operationTimer.ElapsedMilliseconds} ms // event time";
            StatusLabel.Text = $"Moved {graphicsToMove} graphics randomly.";
        }

        async private void OnAddSymbolsClicked(object sender, EventArgs e)
        {
            int count = GetRequestedCount();

            EventTimer.Text = " // event time";
            DrawTimer.Text = " // draw time";
            Stopwatch operationTimer = Stopwatch.StartNew();
            drawClock.Reset();
            drawClock.Start();


            for (int i = 9; i < count; i++)
            {
                int symbolType = random.Next(1, 4);
                string symbolIndex = random.Next(5, 11).ToString();
                Graphic graphic = graphicsOverlay.Graphics[i];
                SimpleMarkerSymbol greenTriangle = new SimpleMarkerSymbol(SimpleMarkerSymbolStyle.Triangle, System.Drawing.Color.Green, 40);

                switch (symbolType)
                {
                    case 1:
                        graphic.Symbol = greenTriangle;
                        break;
                    case 2:
                        MultilayerPointSymbol modelSymbol = await createModelLayerFromFile(symbolIndex, 100);
                        graphic.Symbol = modelSymbol;
                        break;
                    case 3:
                        MultilayerPointSymbol pictureSymbol = await createPictureLayerFromFile(symbolIndex, 100);
                        graphic.Symbol = pictureSymbol;
                        break;
                    default:
                        graphic.Symbol = greenTriangle;
                        break;
                    
                }

            }

            operationTimer.Stop();
            EventTimer.Text = $"{operationTimer.ElapsedMilliseconds} ms // event time";
            StatusLabel.Text = $"Assigned symbols to {count} graphics.";
        }

        private void OnRemoveOverlayClicked(object sender, EventArgs e)
        {
            sceneView.GraphicsOverlays.Remove(graphicsOverlay);

            StatusLabel.Text = $"Removed overlay.";
        }
        private void OnAddOverlayClicked(object sender, EventArgs e)
        {
            sceneView.GraphicsOverlays.Add(graphicsOverlay);

            StatusLabel.Text = $"Added overlay.";
        }

        private void OnRunAnimationsClicked(object sender, EventArgs e)
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
            Stopwatch runTimer = Stopwatch.StartNew();
            drawClock.Reset();
            drawClock.Start();

            double[] runTimes = new double[runsCount];


            for (int i = 0; i < runsCount; i++)
            {
                runTimer.Reset();
                runTimer.Start();
                switch (workflow)
                {
                    case "Move graphics (uniform)":
                        OnMoveGraphicsUniformClicked(sender, e);
                        break;
                    case "Move graphics (random)":
                        OnMoveGraphicsRandomClicked(sender, e);
                        break;
                    case "Swap renderer":
                        OnSwapRendererClicked(sender, e);
                        break;
                    case "Add symbols":
                        OnAddSymbolsClicked(sender, e);
                        break;
                    case "Add/remove overlay":
                        OnRemoveOverlayClicked(sender, e);
                        OnAddOverlayClicked(sender, e);
                        break;
                    case "Add/move/remove":
                        OnAddGraphicsClicked(sender, e);
                        OnMoveGraphicsRandomClicked(sender, e);
                        OnRemoveGraphicsClicked(sender, e);
                        break;
                    case "Add/give symbols/remove":
                        OnAddGraphicsClicked(sender, e);
                        OnAddSymbolsClicked(sender, e);
                        OnRemoveGraphicsClicked(sender, e);
                        break;
                    default:
                        StatusLabel.Text = $"Unknown workflow: {workflow}";
                        return;
                }
                runTimer.Stop();
                runTimes[i] = (double)runTimer.ElapsedMilliseconds;
                // Thread.Sleep(10);
            }

            operationTimer.Stop();
            EventTimer.Text = $"Total time: {operationTimer.ElapsedMilliseconds} ms // slowest time: {runTimes.Max()} // average time: {runTimes.Average()}";
            StatusLabel.Text = $"Completed {runsCount} runs of workflow: {workflow}";
        }

    }
}