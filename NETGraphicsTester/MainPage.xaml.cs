using Esri.ArcGISRuntime.Mapping;
using Esri.ArcGISRuntime.Portal;
using Esri.ArcGISRuntime.Symbology;
using Esri.ArcGISRuntime.Geometry;
using Esri.ArcGISRuntime.UI;
using System.Diagnostics;
using System.Globalization;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using Esri.ArcGISRuntime.Data;
using Esri.ArcGISRuntime.Location;
using ArcLocation = Esri.ArcGISRuntime.Location.Location;
using Esri.ArcGISRuntime.Tasks.Geocoding;
using Esri.ArcGISRuntime.Maui;
using System.Reflection.Metadata;

namespace NETGraphicsTester
{
    public partial class MainPage : ContentPage
    {
        Random random = new Random();
        Graphic? currentlyIdentifiedGraphic;

        string passiveMode = "normal";
        int graphicsCount = 0;

        private bool _sceneReady;
        private readonly LocationDataSource _locationSource = new SystemLocationDataSource();
        // Should be false if using real location
        private bool _useSimulatedLocation = false;
        
        private MapPoint? _latestPosition;
        private Graphic? _positionGraphic;

        private readonly List<Graphic> _routeStops = new();

        private const double TriggerDistanceMeters = 50;

        Surface baseSurface;

        GraphicsOverlay graphicsOverlay = new GraphicsOverlay();
        SimpleRenderer defaultRenderer = new SimpleRenderer(new SimpleMarkerSymbol(SimpleMarkerSymbolStyle.Circle, System.Drawing.Color.Yellow, 15));
        Graphic? droneGraphic;
        private const double NearbyRadiusMeters = 800;
        private static readonly HttpClient OverpassHttpClient = new();
        private readonly Queue<MapPoint> _droneDestinations = new();
        private CancellationTokenSource? _workflowCancellation;
        private bool _isDroneAnimating;

        private sealed record PointSnapShot(
            MapPoint Point,
            Dictionary<string, object> Attributes,
            string Signature);

        public MainPage()
        {
            InitializeComponent();
            _ = InitializeSceneAsync();
        }

        private void SceneView_DrawStatusChanged(object? sender, EventArgs e)
        {
            if (sceneView.DrawStatus == DrawStatus.Completed)
            {
                Debug.WriteLine("Draw completed.");
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
            try
            {
                var localSceneView = sceneView;
                if (localSceneView == null)
                {
                    Debug.WriteLine("Scene view is not ready.");
                    return;
                }

                localSceneView.WarningsChanged += SceneView_WarningsChanged;

                var scene = new Scene(SceneViewingMode.Local, BasemapStyle.ArcGISChartedTerritory);
                var camera = new Camera(37.7, -122.4194, 15000, 0, 30, 0);


                string _elevationServiceUrl = "https://elevation3d.arcgis.com/arcgis/rest/services/WorldElevation3D/Terrain3D/ImageServer";
                baseSurface = new Surface();
                ArcGISTiledElevationSource elevationSource = new ArcGISTiledElevationSource(new Uri(_elevationServiceUrl));
                baseSurface.ElevationSources.Add(elevationSource);
                scene.BaseSurface = baseSurface;

                await scene.LoadAsync();

                scene.BaseSurface.NavigationConstraint = NavigationConstraint.None;
                graphicsOverlay.Renderer = defaultRenderer;
                localSceneView.GraphicsOverlays?.Add(graphicsOverlay);

                localSceneView.Scene = scene;

                localSceneView.SetViewpointCamera(camera);
                localSceneView.DrawStatusChanged += SceneView_DrawStatusChanged;
                localSceneView.GeoViewTapped += OnSceneViewTapped_Default;

                _sceneReady = true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error thrown: {ex.Message}");
            }
        }

        async private Task AddGraphicsOperation(Geometry geometry)
        {
            Graphic graphic = new Graphic(geometry);
            graphicsOverlay.Graphics.Add(graphic);
        }

        private async Task AddRandomGraphics(int count)
        {
            Envelope? extent = sceneView.GetCurrentViewpoint(ViewpointType.BoundingGeometry)?.TargetGeometry as Envelope;
            MapPoint? center = sceneView.GetCurrentViewpoint(ViewpointType.CenterAndScale)?.TargetGeometry as MapPoint;
            double baseZ = center?.Z ?? 0;
            const double zRange = 500;

            if (extent == null || center == null)
            {
                Debug.WriteLine("Extent or center is null");
                return;
            }

            for (int i = 0; i < count; i++)
            {
                double x = extent.XMin + (random.NextDouble() * (extent.XMax - extent.XMin));
                double y = extent.YMin + (random.NextDouble() * (extent.YMax - extent.YMin));
                double z = baseZ + ((random.NextDouble() * 2 * zRange) - zRange);
                MapPoint point = new MapPoint(x, y, z, extent.SpatialReference);

                Graphic graphic = new Graphic(point);
                graphicsOverlay.Graphics.Add(graphic);
            }
        }
        private void RemoveGraphicsOperation(int? count = null, int? index = null)
        {
            if (count.HasValue)
            {
                int removeCount = Math.Min(count.Value, graphicsOverlay.Graphics.Count);
                for (int i = 0; i < removeCount; i++)
                {
                    graphicsOverlay.Graphics.RemoveAt(graphicsOverlay.Graphics.Count - 1);
                }
            }
            if (index.HasValue)
            {
                if (index.Value >= 0 && index.Value < graphicsOverlay.Graphics.Count)
                {
                    graphicsOverlay.Graphics.RemoveAt(index.Value);
                }
            }
        }


        async private Task<MultilayerPointSymbol> createModelLayerFromFile(string fileName, int size = 50)
        {
            string file = $"{fileName}.glb";

            string filePath = Path.Combine(FileSystem.CacheDirectory, file);
            {
                using var sourceStream = await FileSystem.OpenAppPackageFileAsync(file);
                using var destStream = File.Create(filePath);
                await sourceStream.CopyToAsync(destStream);
            }

            ModelSymbolLayer modelLayer = new ModelSymbolLayer(new Uri(filePath, UriKind.Absolute));
            await modelLayer.LoadAsync();
            Debug.WriteLine($"{file} default dimensions: height={modelLayer.Height}, width={modelLayer.Width}, depth={modelLayer.Depth}");

            MultilayerPointSymbol multilayerSymbol = new MultilayerPointSymbol(new SymbolLayer[] { modelLayer })
            {
                SceneSizeUnit = SymbolSizeUnits.Meters,
                Size = size
            };

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


        private void MoveGraphics(int index, Geometry newGeometry)
        {
            if (index < 0 || index >= graphicsOverlay.Graphics.Count)
            {
                return;
            }

            Graphic graphic = graphicsOverlay.Graphics[index];
            graphic.Geometry = newGeometry;
        }

        private void ToggleGraphicsVisibility(int index)
        {
            if (graphicsOverlay.Graphics[index].IsVisible)
            {
                graphicsOverlay.Graphics[index].IsVisible = false;
            } else
            {
                graphicsOverlay.Graphics[index].IsVisible = true;
            }
        }

        private async void OnSceneViewTapped_Default(object? sender, Esri.ArcGISRuntime.Maui.GeoViewInputEventArgs e)
        {
            Debug.WriteLine($"Scene view tapped at position: {e.Position}");
        }

        private async Task<bool> ResetAndClear()
        {
            if (!_sceneReady)
            {
                Debug.WriteLine("Cannot reset graphics: the scene is not ready.");
                return false;
            }

            var overlays = sceneView.GraphicsOverlays;
            if (overlays is null)
            {
                Debug.WriteLine("Cannot reset graphics: overlays are unavailable.");
                return false;
            }

            sceneView.GeoViewTapped -= OnSceneViewTapped_AddGraphics;
            sceneView.GeoViewTapped -= OnSceneViewTapped_IdentifyOn;
            sceneView.GeoViewDoubleTapped -= OnSceneViewDoubleTapped_MoveLocation;

            _workflowCancellation?.Cancel();
            _workflowCancellation?.Dispose();
            _workflowCancellation = null;
            _droneDestinations.Clear();
            droneGraphic = null;
            _isDroneAnimating = false;

            _latestPosition = null;
            _positionGraphic = null;
            _routeStops.Clear();

            graphicsOverlay.Graphics.Clear();

            var replacement = new GraphicsOverlay
            {
                Renderer = new SimpleRenderer(
                    new SimpleMarkerSymbol(
                        SimpleMarkerSymbolStyle.Circle,
                        System.Drawing.Color.Yellow,
                        15))
            };

            overlays.Clear();
            graphicsOverlay = replacement;
            overlays.Add(graphicsOverlay);

            _locationSource.LocationChanged -= OnLocationChanged;
            await _locationSource.StopAsync();

            graphicsCount = 0;
            passiveMode = "normal";

            return true;
        }

        private async void ExecuteWorkflow_ModelLODs(object? sender, EventArgs e)
        {
            if (!await ResetAndClear()) return;
            Debug.WriteLine("[JS WORKFLOW 1] Use graphic to temporarily preview one LOD of a 3d model");

            passiveMode = "modelLODs";
            sceneView.GeoViewTapped += OnSceneViewTapped_AddGraphics;
        }

        private async void ExecuteWorkflow_DataExtraction(object? sender, EventArgs e)
        {
            if (!await ResetAndClear()) return;
            Debug.WriteLine("[JS WORKFLOW 2] Graphics as extraction points for underlying data");
            passiveMode = "dataExtraction";

            sceneView.GeoViewTapped += OnSceneViewTapped_AddGraphics;
        }

        private async void ExecuteWorkflow_EditingExport(object? sender, EventArgs e)
        {
            if (!await ResetAndClear()) return;
            Debug.WriteLine("[JS WORKFLOW 3] Graphics as editing/sketch workflow, export of features");
            passiveMode = "editingExport";
            sceneView.GeoViewTapped += OnSceneViewTapped_AddGraphics;
        }

        private async void ExecuteWorkflow_SelectionHighlight(object? sender, EventArgs e)
        {
            if (!await ResetAndClear()) return;
            Debug.WriteLine("[JS WORKFLOW 4] Selection highlight as separate overlay, using a feature table as source");
            passiveMode = "selectionHighlight";

            await ImportGraphicsFromServiceAsync("https://services5.arcgis.com/N82JbI5EYtAkuUKU/arcgis/rest/services/SF_Points_3D/FeatureServer/12");

            // Turn on highlighting behavior
            sceneView.GeoViewTapped += OnSceneViewTapped_IdentifyOn;
        }

        private async void ExecuteWorkflow_SatelliteOrbit(object? sender, EventArgs e)
        {
            if (!await ResetAndClear()) return;
            Debug.WriteLine("[JS WORKFLOW 5] Satellite orbit- high altitude tracks, asynchronous movement");

            var workflow = new CancellationTokenSource();
            _workflowCancellation = workflow;
            CancellationToken token = workflow.Token;

            GraphicsOverlay satelliteOverlay = graphicsOverlay;
            satelliteOverlay.SceneProperties.SurfacePlacement = SurfacePlacement.Absolute;

            try
            {
                await AnimateSatelliteOrbitAsync(satelliteOverlay, token);
            }
            catch (OperationCanceledException) when (token.IsCancellationRequested)
            {
                Debug.WriteLine("Satellite animation stopped.");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Satellite animation failed: {ex}");
                await DisplayAlert("Satellite Orbit", ex.Message, "OK");
            }
        }

        private async void ExecuteWorkflow_MoveFormation(object? sender, EventArgs e)
        {
            if (!await ResetAndClear()) return;
            Debug.WriteLine("[JS WORKFLOW 6] Move a constellation or formation of features");
            passiveMode = "moveFormation1";
            sceneView.GeoViewTapped += OnSceneViewTapped_AddGraphics;
        }

        private async void ExecuteWorkflow_RealTimeAircraft(object? sender, EventArgs e)
        {
            if (!await ResetAndClear()) return;
            Debug.WriteLine("[JS WORKFLOW 7] \"Real-time\" moving aircraft with heading, ephemeral lightning graphics");

            var workflow = new CancellationTokenSource();
            _workflowCancellation = workflow;
            CancellationToken token = workflow.Token;

            GraphicsOverlay aircraftOverlay = graphicsOverlay;
            aircraftOverlay.SceneProperties.SurfacePlacement = SurfacePlacement.Absolute;

            try
            {
                using var stream = await FileSystem.OpenAppPackageFileAsync("airplane_renderer.json");
                using var reader = new StreamReader(stream);
                string rendererJson = await reader.ReadToEndAsync();

                token.ThrowIfCancellationRequested();
                aircraftOverlay.Renderer = Renderer.FromJson(rendererJson);

                var sourceOverlay = new GraphicsOverlay();
                await ReadGeoJsonPoints("SF_points.geojson", sourceOverlay);

                token.ThrowIfCancellationRequested();

                MapPoint[] destinations = sourceOverlay.Graphics
                    .Select(graphic => graphic.Geometry is MapPoint point 
                        ? (MapPoint)GeometryEngine.Project(point, SpatialReferences.WebMercator) 
                        : throw new InvalidOperationException("Aircraft destinations must be points"))
                    .DistinctBy(point => (point.X, point.Y))
                    .ToArray();

                if (destinations.Length < 2)
                    throw new InvalidOperationException("At least two aircraft destinations are required.");

                await AnimateAircraftAsync(aircraftOverlay, destinations, token);
            }
            catch (OperationCanceledException) when (token.IsCancellationRequested)
            {
                Debug.WriteLine("Aircraft animation stopped.");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Aircraft animation failed: {ex}");
                await DisplayAlert("Real-Time Aircraft", ex.Message, "OK");
            }
        }

        private async void ExecuteWorkflow_MetroMarkers(object? sender, EventArgs e)
        {
            if (!await ResetAndClear()) return;
            Debug.WriteLine("[JS WORKFLOW 8] Moving metro markers, callouts when approaching stations");

            var workflow = new CancellationTokenSource();
            _workflowCancellation = workflow;
            CancellationToken token = workflow.Token;

            GraphicsOverlay busOverlay = graphicsOverlay;
            busOverlay.SceneProperties.SurfacePlacement = SurfacePlacement.Relative;
            using var stream = await FileSystem.OpenAppPackageFileAsync("bus.json");
            using var reader = new StreamReader(stream);
            string rendererJson = await reader.ReadToEndAsync();

            graphicsOverlay.Renderer = Renderer.FromJson(rendererJson);

            try
            {
                

                token.ThrowIfCancellationRequested();

                var stopOverlay = new GraphicsOverlay
                {
                    Renderer = new SimpleRenderer(new SimpleMarkerSymbol(SimpleMarkerSymbolStyle.Circle, System.Drawing.Color.Gray, 8))
                };
                stopOverlay.SceneProperties.SurfacePlacement = SurfacePlacement.Relative;
                
                await ReadGeoJsonPoints("SF_points.geojson", stopOverlay);
                token.ThrowIfCancellationRequested();

                var stops = stopOverlay.Graphics.Select(graphic =>
                {
                    if (graphic.Geometry is not MapPoint point)
                    {
                        throw new InvalidOperationException("Bus stops must be points");
                    }

                    MapPoint projected = (MapPoint)GeometryEngine.Project(point, SpatialReferences.WebMercator);

                    string id = Convert.ToString(
                        graphic.Attributes["OBJECTID"],
                        CultureInfo.InvariantCulture)
                        ?? throw new InvalidOperationException("Stop ID is missing");
                    
                    return (Point: projected, Id: id);
                }).ToArray();

                if (stops.Length == 0)
                {
                    throw new InvalidOperationException("No bus stops were loaded");
                }

                sceneView.GraphicsOverlays.Add(stopOverlay);

                await AnimateBusesAsync(busOverlay, stops, token);
            }
            catch (OperationCanceledException) when (token.IsCancellationRequested)
            {
                Debug.WriteLine("Bus animation stopped.");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Bus animation failed: {ex}");
                await DisplayAlert("Metro Markers", ex.Message, "OK");
            }
        }
        private async void ExecuteWorkflow_MissileTrajectory(object? sender, EventArgs e)
        {
            if (!await ResetAndClear()) return;
            Debug.WriteLine("[.NET WORKFLOW 1] Choose two points as calc input, show missile trajectory");
            passiveMode = "missileTrajectory";
            sceneView.GeoViewTapped += OnSceneViewTapped_AddGraphics;
        }

        private async void ExecuteWorkflow_WebRenderer(object? sender, EventArgs e)
        {
            if (!await ResetAndClear()) return;
            Debug.WriteLine("[.NET WORKFLOW 2] Class based + size based renderer from web fetched json");

            await ReadGeoJsonPoints("SF_points.geojson");

            await ApplySymbols("webStyles");
        }

        private async void ExecuteWorkflow_CreateTrack(object? sender, EventArgs e)
        {
            if (!await ResetAndClear()) return;
            Debug.WriteLine("[.NET WORKFLOW 3] Create track (GPX) from location and search for POI from service (open street map?)");

            passiveMode = "createTrack";

            var workflow = new CancellationTokenSource();
            _workflowCancellation = workflow;
            CancellationToken token = workflow.Token;

            try
            {
                await StartTestLocationAsync(token);

                using var timer = new PeriodicTimer(TimeSpan.FromSeconds(2));

                while (await timer.WaitForNextTickAsync(token))
                {
                    token.ThrowIfCancellationRequested();

                    if (_latestPosition is not MapPoint position)
                    {
                        Debug.WriteLine("Waiting for first location");
                        continue;
                    }

                    var breadcrumb = new Graphic(
                        position,
                        new SimpleMarkerSymbol(SimpleMarkerSymbolStyle.Circle, System.Drawing.Color.Orange, 10)
                    );

                    breadcrumb.Attributes["recordedAtUtc"] = DateTimeOffset.UtcNow.ToString("O");

                    graphicsOverlay.Graphics.Add(breadcrumb);
                }
            }
            catch (OperationCanceledException) when (token.IsCancellationRequested)
            {
                Debug.WriteLine("Track recording stopped.");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Track recording failed: {ex}");
                await DisplayAlert("Create Track", ex.Message, "OK");
            }
        }

        private async void ExecuteWorkflow_FlashingUpdates(object? sender, EventArgs e)
        {
            if (!await ResetAndClear()) return;
            Debug.WriteLine("[.NET WORKFLOW 4] Flashing graphics for updated data, polling rest endpoint");

            passiveMode = "flashingUpdates";

            var workflow = new CancellationTokenSource();
            _workflowCancellation = workflow;
            CancellationToken token = workflow.Token;

            GraphicsOverlay targetOverlay = graphicsOverlay;
            var displayed = new Dictionary<long, Graphic>();
            var previous = new Dictionary<long, PointSnapShot>();
            bool initialized = false;

            try
            {
                using var timer = new PeriodicTimer(TimeSpan.FromSeconds(5));

                do
                {
                    var current = await ReadPointSnapshotAsync(token);
                    token.ThrowIfCancellationRequested();

                    var changedLocations = new List<MapPoint>();

                    foreach (var (id, state) in current)
                    {
                        if (!displayed.TryGetValue(id, out Graphic? graphic))
                        {
                            graphic = new Graphic(
                                state.Point,
                                state.Attributes,
                                new SimpleMarkerSymbol(SimpleMarkerSymbolStyle.Circle, System.Drawing.Color.Yellow, 15)
                            );

                            displayed.Add(id, graphic);
                            targetOverlay.Graphics.Add(graphic);

                            if (initialized)
                            {
                                changedLocations.Add(state.Point);
                            }
                        }
                        else if (previous[id].Signature != state.Signature)
                        {
                            graphic.Geometry = state.Point;
                            graphic.Attributes.Clear();

                            foreach (var attribute in state.Attributes)
                            {
                                graphic.Attributes[attribute.Key] = attribute.Value;
                            }

                            changedLocations.Add(state.Point);
                        }
                    }

                    foreach (long id in previous.Keys.Except(current.Keys))
                    {
                        changedLocations.Add(previous[id].Point);
                        targetOverlay.Graphics.Remove(displayed[id]);
                        displayed.Remove(id);
                    }

                    previous = current;
                    initialized = true;

                    if (changedLocations.Count > 0)
                    {
                        await FlashPointsAsync(targetOverlay, changedLocations, token);
                    }
                }
                while(await timer.WaitForNextTickAsync(token));
            }
            catch (OperationCanceledException) when (token.IsCancellationRequested)
            {
                Debug.WriteLine("Feature polling stopped.");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Feature polling failed: {ex}");
                await DisplayAlert("Flashing Updates", ex.Message, "OK");
            }
        }

        private async void ExecuteWorkflow_GeocodePoints(object? sender, EventArgs e)
        {
            if (!await ResetAndClear()) return;
            Debug.WriteLine("[FLUTTER WORKFLOW 2] Geocoded points or taps");

            passiveMode = "geocode";
            sceneView.GeoViewTapped += OnSceneViewTapped_AddGraphics;
        }

        private async void ExecuteWorkflow_TransferOverlay(object? sender, EventArgs e)
        {
            if (!await ResetAndClear()) return;
            Debug.WriteLine("[FLUTTER WORKFLOW 3] Transfer overlay between map/scene");

            await ImportGraphicsFromServiceAsync("https://services5.arcgis.com/N82JbI5EYtAkuUKU/arcgis/rest/services/SF_Points_3D/FeatureServer/12");
            await TransferOverlayToMap();
        }

        private async void ExecuteWorkflow_QueryByExtent(object? sender, EventArgs e)
        {
            if (!await ResetAndClear()) return;
            Debug.WriteLine("[FLUTTER WORKFLOW 4] Query by extent and draw data");

            await ImportGraphicsFromServiceAsync("https://services5.arcgis.com/N82JbI5EYtAkuUKU/arcgis/rest/services/SF_Points_3D/FeatureServer/12", "extent");
        }

        private async void ExecuteWorkflow_FilterGraphics(object? sender, EventArgs e)
        {
            if (!await ResetAndClear()) return;
            Debug.WriteLine("[FLUTTER WORKFLOW 5] Load graphics from feature service, apply filters by search");

            await ImportGraphicsFromServiceAsync("https://services5.arcgis.com/N82JbI5EYtAkuUKU/arcgis/rest/services/SF_Points_3D/FeatureServer/12");

            string jsonString = File.ReadAllText("unique_value_renderer.json");
            Renderer uniqueValueRenderer = Renderer.FromJson(jsonString);
            graphicsOverlay.Renderer = uniqueValueRenderer;

            Thread.Sleep(3000);

            graphicsOverlay.Graphics.Clear();
            Debug.WriteLine("Applying filter for graphics of type 1");
            await ImportGraphicsFromServiceAsync("https://services5.arcgis.com/N82JbI5EYtAkuUKU/arcgis/rest/services/SF_Points_3D/FeatureServer/12", "class_value = 1");
        }

        private async void ExecuteWorkflow_MultifacetedRenderer(object? sender, EventArgs e)
        {
            if (!await ResetAndClear()) return;
            Debug.WriteLine("[QT WORKFLOW 1] Complex multifaceted renderer based on selection location");

            await ReadGeoJsonPoints("SF_points.geojson");

            await ApplySymbols("multifaceted");
        }

        private async void ExecuteWorkflow_GeoJSONGraphics(object? sender, EventArgs e)
        {
            if (!await ResetAndClear()) return;
            Debug.WriteLine("[QT WORKFLOW 2] Graphics from geojson");

            await ReadGeoJsonPoints("SF_points.geojson");
        }

        private async void ExecuteWorkflow_TraverseRoute(object? sender, EventArgs e)
        {
            if (!await ResetAndClear()) return;
            Debug.WriteLine("[QT WORKFLOW 3] Route pins, switch to traversed based on location traveled.");
            passiveMode = "traverseRoute";
            await ReadGeoJsonPoints("SF_points.geojson");

            var workflow = new CancellationTokenSource();
            _workflowCancellation = workflow;
            CancellationToken token = workflow.Token;

            List<Graphic> graphics = graphicsOverlay.Graphics.ToList();
            foreach (var graphic in graphics)
            {
                graphic.Attributes["visited"] = false;
                _routeStops.Add(graphic);
            }

            try
            {
                await StartTestLocationAsync(token);
            }
            catch (OperationCanceledException) when (token.IsCancellationRequested)
            {
                Debug.WriteLine("Route test stopped.");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Route test failed: {ex}");
                await DisplayAlert("Traverse Route", ex.Message, "OK");
            }
        }

        private async void ExecuteWorkflow_ComplexPopup(object? sender, EventArgs e)
        {
            if (!await ResetAndClear()) return;
            Debug.WriteLine("[QT WORKFLOW 4] Graphic identify and complex popup with embedded images");
            await ReadJsonPoints("SF_Park_Points.json");
            passiveMode = "complexPopup";
            sceneView.GeoViewTapped += OnSceneViewTapped_IdentifyOn;
        }

        private async void ExecuteWorkflow_TextLabels(object? sender, EventArgs e)
        {
            if (!await ResetAndClear()) return;
            Debug.WriteLine("[QT WORKFLOW 5] Labels as text symbols");

            await ReadGeoJsonPoints("SF_points.geojson");
            await ApplySymbols("text");
            await ReadGeoJsonPoints("SF_points.geojson");
        }

        private async void ExecuteWorkflow_FollowGraphic(object? sender, EventArgs e)
        {
            if (!await ResetAndClear()) return;
            Debug.WriteLine("[QT WORKFLOW 6] Following a graphic");

            MapPoint startPoint = new MapPoint(-13634471.9113, 4550391.4384, SpatialReferences.WebMercator);
            graphicsOverlay.Graphics.Add(new Graphic(startPoint));

            MapPoint endPoint = new MapPoint(-13628102.2396, 4548508.0037, SpatialReferences.WebMercator);
            graphicsOverlay.Graphics.Add(new Graphic(endPoint));

            createMissileTrajectory();

            SimpleMarkerSymbol followSymbol = new SimpleMarkerSymbol(SimpleMarkerSymbolStyle.Cross, System.Drawing.Color.Pink, 30);
            Graphic followGraphic = new Graphic(startPoint, followSymbol);

            graphicsOverlay.Graphics.Add(followGraphic);

            var trackGraphics = sceneView.GraphicsOverlays.ElementAt(1).Graphics;

            foreach (var graphic in trackGraphics)
            {
                var geometry = graphic.Geometry;
                if (geometry is MapPoint mapPoint)
                {
                    followGraphic.Geometry = mapPoint;
                    sceneView.SetViewpoint(new Viewpoint(mapPoint));
                }
                Thread.Sleep(10);
            }
        }

        private async void ExecuteWorkflow_ShowNearbyPoints(object? sender, EventArgs e)
        {
            if (!await ResetAndClear()) return;
            Debug.WriteLine("[SWIFT WORKFLOW 1] Show POI points nearby location");
            passiveMode = "showNearbyPoints";
            _workflowCancellation = new CancellationTokenSource();
            sceneView.GeoViewTapped += OnSceneViewTapped_AddGraphics;
        }

        private async void ExecuteWorkflow_DroneTrack(object? sender, EventArgs e)
        {
            if (!await ResetAndClear()) return;
            Debug.WriteLine("[SWIFT WORKFLOW 2] Drone flying by track with location, tap to move there");
            passiveMode = "droneTrack";

            var workflowCancellation = new CancellationTokenSource();
            _workflowCancellation = workflowCancellation;

            try
            {
                string droneSymbolJson = await File.ReadAllTextAsync("drone.json");
                Symbol droneSymbol = Symbol.FromJson(droneSymbolJson);
                MapPoint startPoint = new MapPoint(-13628102.2396, 4548508.0037, SpatialReferences.WebMercator);
                droneGraphic = new Graphic(startPoint, droneSymbol);
                graphicsOverlay.Graphics.Add(droneGraphic);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Unable to load the drone symbol: {ex}");
                return;
            }

            sceneView.GeoViewTapped += OnSceneViewTapped_AddGraphics;
        }

        private async void ExecuteWorkflow_MovingOrbit(object? sender, EventArgs e)
        {
            if (!await ResetAndClear()) return;
            Debug.WriteLine("[AI WORKFLOW 1] 50,000 moving graphics, continuous orbiting camera");
        }

        private async void ExecuteWorkflow_StackedGraphics(object? sender, EventArgs e)
        {
            if (!await ResetAndClear()) return;
            Debug.WriteLine("[AI WORKFLOW 2] Large number of graphics at same x,y with incremental z differences");
        }

        private async void ExecuteWorkflow_OffscreenGraphics(object? sender, EventArgs e)
        {
            if (!await ResetAndClear()) return;
            Debug.WriteLine("[AI WORKFLOW 3] Many graphics added off screen");
        }

        private async void ExecuteWorkflow_CrossBoundary(object? sender, EventArgs e)
        {
            if (!await ResetAndClear()) return;
            Debug.WriteLine("[AI WORKFLOW 4] Moving graphics across clipping area boundaries");
        }

        private async void OnSceneViewTapped_AddGraphics(object sender, GeoViewInputEventArgs e)
        {
            if (e.Location is not MapPoint tappedLocation)
            {
                Debug.WriteLine("Tap did not return a valid location");
                return;
            }
            Graphic graphic = new Graphic(tappedLocation);

            graphicsOverlay.Graphics.Add(graphic);
            graphicsCount++;

            switch(passiveMode)
            {
                case "editingExport":
                    if (graphicsCount > 4)
                    {
                        Debug.WriteLine("Maximum graphics count reached.");
                        sceneView.GeoViewTapped -= OnSceneViewTapped_AddGraphics;
                        ExportGraphics();

                        return;
                    }
                    break;
                case "missileTrajectory":
                    if (graphicsCount == 2)
                    {
                        Debug.WriteLine("Maximum graphics count reached.");
                        sceneView.GeoViewTapped -= OnSceneViewTapped_AddGraphics;
                        createMissileTrajectory();

                        return;
                    }
                    break;
                case "dataExtraction":
                    double elevation = await baseSurface.GetElevationAsync(graphic.Geometry as MapPoint);
                    sceneView.GeoViewTapped -= OnSceneViewTapped_AddGraphics;

                    var textSymbol = new TextSymbol(elevation.ToString(), System.Drawing.Color.Red, 12, Esri.ArcGISRuntime.Symbology.HorizontalAlignment.Left, Esri.ArcGISRuntime.Symbology.VerticalAlignment.Bottom);
                    graphicsOverlay.Graphics.Add(new Graphic(graphic.Geometry, textSymbol));

                    Debug.WriteLine($"Elevation at tapped location: {elevation}");
                    break;
                case "geocode":
                    string address = await GetAddressAsync(tappedLocation);
                    var addressLabel = new TextSymbol(address, System.Drawing.Color.Red, 12, Esri.ArcGISRuntime.Symbology.HorizontalAlignment.Left, Esri.ArcGISRuntime.Symbology.VerticalAlignment.Bottom);
                    Graphic addressGraphic = new Graphic(tappedLocation, addressLabel);
                    graphicsOverlay.Graphics.Add(addressGraphic);
                    break;
                case "modelLODs":
                    if (graphicsCount > 2)
                    {
                        await ApplySymbols("modelLODs");
                        sceneView.GeoViewTapped -= OnSceneViewTapped_AddGraphics;
                    }
                    break;
                case "showNearbyPoints":
                {
                    sceneView.GeoViewTapped -= OnSceneViewTapped_AddGraphics;
                    graphic.Symbol = new SimpleMarkerSymbol(SimpleMarkerSymbolStyle.Circle, System.Drawing.Color.Orange, 18);
                    MapPoint center = (MapPoint)GeometryEngine.Project(tappedLocation, SpatialReferences.Wgs84);
                    GraphicsOverlay targetOverlay = graphicsOverlay;
                    CancellationToken token = _workflowCancellation?.Token ?? CancellationToken.None;

                    await ShowNearbyPoiAsync(center, targetOverlay, token);
                    break;
                }
                case "droneTrack":
                {
                    MapPoint nextLocation = (MapPoint)GeometryEngine.Project(tappedLocation, SpatialReferences.WebMercator);
                    graphic.Geometry = nextLocation;
                    graphic.Symbol = new SimpleMarkerSymbol(SimpleMarkerSymbolStyle.Circle, System.Drawing.Color.Cyan, 12);

                    _droneDestinations.Enqueue(nextLocation);
                    if (!_isDroneAnimating && droneGraphic is not null && _workflowCancellation is not null)
                    {
                        await AnimateDroneDestinationsAsync(droneGraphic, _workflowCancellation);
                    }
                    break;
                }
                case "moveFormation1":
                {
                    double offset = 50;
                    MapPoint point1 = new MapPoint(tappedLocation.X + offset, tappedLocation.Y + offset, tappedLocation.Z, tappedLocation.SpatialReference);
                    MapPoint point2 = new MapPoint(tappedLocation.X - offset, tappedLocation.Y + offset, tappedLocation.Z, tappedLocation.SpatialReference);
                    MapPoint point3 = new MapPoint(tappedLocation.X + offset, tappedLocation.Y - offset, tappedLocation.Z, tappedLocation.SpatialReference);
                    MapPoint point4 = new MapPoint(tappedLocation.X - offset, tappedLocation.Y - offset, tappedLocation.Z, tappedLocation.SpatialReference);

                    graphicsOverlay.Graphics.Add(new Graphic(point1));
                    graphicsOverlay.Graphics.Add(new Graphic(point2));
                    graphicsOverlay.Graphics.Add(new Graphic(point3));
                    graphicsOverlay.Graphics.Add(new Graphic(point4));
                    break;
                }
                case "moveFormation2":
                {
                    List<Graphic> graphicPoints = graphicsOverlay.Graphics.ToList();                    
                    MapPoint oldPoint = graphicPoints[0].Geometry as MapPoint;

                    double deltaX = tappedLocation.X - oldPoint.X;
                    double deltaY = tappedLocation.Y - oldPoint.Y;

                    for (int i = 1; i < graphicPoints.Count; i ++)
                    {
                        MapPoint currentPoint = graphicPoints[i].Geometry as MapPoint;
                        if (currentPoint is not null)
                        {
                            MapPoint newPoint = new MapPoint(
                                currentPoint.X + deltaX,
                                currentPoint.Y + deltaY,
                                currentPoint.Z,
                                currentPoint.SpatialReference);
                            graphicPoints[i].Geometry = newPoint;
                        }
                    }
                    break;
                }
                default:
                    break;
            }
        }

        private async void OnSceneViewTapped_IdentifyOn(object sender, GeoViewInputEventArgs e)
        {
            double tolerance = 10.0;
            int maximumResults = 5;
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
                    var attributes = identifiedGraphic.Attributes;

                    switch (passiveMode)
                    {
                        case "selectionHighlight":
                            GraphicsOverlay highlightOverlay = new GraphicsOverlay();
                            SimpleRenderer highlightRenderer = new SimpleRenderer(new SimpleMarkerSymbol(SimpleMarkerSymbolStyle.Cross, System.Drawing.Color.Cyan, 10));
                            highlightOverlay.Renderer = highlightRenderer;
                            Graphic highlightedGraphic = new Graphic(identifiedGraphic.Geometry);

                            highlightOverlay.Graphics.Add(highlightedGraphic);
                            sceneView.GraphicsOverlays.Add(highlightOverlay);
                            break;
                        case "complexPopup":
                            string parkName = attributes.ContainsKey("Name") ? attributes["Name"].ToString() : "Unknown park";
                            string parkDescription = attributes.ContainsKey("Description") ? attributes["Description"].ToString() : "No description available";

                            var content = new VerticalStackLayout
                            {
                                Padding = 20,
                                Spacing = 12
                            };

                            content.Children.Add(new Label
                            {
                                Text = parkName,
                                FontSize = 24,
                                FontAttributes = FontAttributes.Bold
                            });

                            content.Children.Add(new Label { Text = parkDescription });

                            if (attributes.TryGetValue("Picture", out var picture) && picture is string dataUri && !string.IsNullOrWhiteSpace(dataUri))
                            {
                                int comma = dataUri.IndexOf(',');
                                if (comma < 0 ||
                                            !dataUri.StartsWith("data:image/", StringComparison.OrdinalIgnoreCase) ||
                                            !dataUri.Substring(0, comma).EndsWith(";base64", StringComparison.OrdinalIgnoreCase))
                                {
                                    throw new FormatException("Park picture is not a base64 image data URI.");
                                }

                                byte[] imageBytes = Convert.FromBase64String(dataUri.Substring(comma + 1));
                                content.Children.Add(new Image
                                {
                                    Source = ImageSource.FromStream(() => new MemoryStream(imageBytes)),
                                    HeightRequest = 250,
                                    Aspect = Aspect.AspectFit
                                });
                            }
                            else
                            {
                                throw new FormatException("Park picture is missing or empty.");
                            }

                            var closeButton = new Button { Text = "Close" };
                            closeButton.Clicked += async (_, _) => await Navigation.PopModalAsync();
                            content.Children.Add(closeButton);

                            var popupPage = new ContentPage
                            {
                                Title = parkName,
                                Content = new ScrollView { Content = content }
                            };

                            await Navigation.PushModalAsync(popupPage);                        
                            break;
                        default:
                            identifiedGraphic.IsSelected = !identifiedGraphic.IsSelected;
                            break;
                    }
                }
                else
                {
                    Debug.WriteLine("Zero graphics identified.");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error identifying graphics: {ex.Message}");
            }
        }

        private void OnSceneViewDoubleTapped_MoveLocation(object? sender, GeoViewInputEventArgs e)
        {
            if (e.Location is MapPoint point)
            {
                UpdateTestPosition(point);
            }
        }

        private void UpdateTestPosition(MapPoint point)
        {
            _latestPosition = (MapPoint)GeometryEngine.Project(point, SpatialReferences.Wgs84);

            if (_positionGraphic is null)
            {
                _positionGraphic = new Graphic(_latestPosition, new SimpleMarkerSymbol(SimpleMarkerSymbolStyle.Diamond, System.Drawing.Color.Blue, 20));

                graphicsOverlay.Graphics.Add(_positionGraphic);
            }
            else
            {
                _positionGraphic.Geometry = _latestPosition;
            }

            if (passiveMode == "traverseRoute")
            {
                CheckRouteStops(_latestPosition);
            }
        }

        private async Task ApplySymbols(string symbolType)
        {
            List<Graphic> graphics = graphicsOverlay.Graphics.ToList();

            foreach (var graphic in graphics)
            {
                var attributes = graphic.Attributes;

                if (symbolType == "multifaceted")
                {
                    SimpleMarkerSymbol highSymbol = new SimpleMarkerSymbol(SimpleMarkerSymbolStyle.Diamond, System.Drawing.Color.Green, 20);
                    SimpleMarkerSymbol mediumSymbol = new SimpleMarkerSymbol(SimpleMarkerSymbolStyle.Diamond, System.Drawing.Color.Yellow, 15);
                    SimpleMarkerSymbol lowSymbol = new SimpleMarkerSymbol(SimpleMarkerSymbolStyle.Diamond, System.Drawing.Color.Orange, 10);

                    if (Convert.ToInt32(attributes["class_value"]) > 3 && Convert.ToInt32(attributes["att_value"]) > 80)
                    {
                        graphic.Symbol = highSymbol;
                    }
                    else if (Convert.ToInt32(attributes["class_value"]) > 1 && Convert.ToInt32(attributes["att_value"]) > 40)
                    {
                        graphic.Symbol = mediumSymbol;
                    }
                    else
                    {
                        graphic.Symbol = lowSymbol;
                    }
                }
                else if (symbolType == "text")
                {
                    string classValue = attributes["class_value"].ToString();
                    string textLabel;

                    switch (classValue)
                    {
                        case "1":
                            textLabel = "one";
                            break;
                        case "2":
                            textLabel = "two";
                            break;
                        case "3":
                            textLabel = "three";
                            break;
                        case "4":
                            textLabel = "four";
                            break;
                        case "5":
                            textLabel = "five";
                            break;
                        default:
                            textLabel = "unknown";
                            break;
                    }
                    graphic.Symbol = new TextSymbol(textLabel, System.Drawing.Color.Black, 12, Esri.ArcGISRuntime.Symbology.HorizontalAlignment.Right, Esri.ArcGISRuntime.Symbology.VerticalAlignment.Bottom);
                }
                else if (symbolType == "webStyles")
                {
                    int classValue = Convert.ToInt32(attributes["class_value"]);
                    int attValue = Convert.ToInt32(attributes["att_value"]);

                    var stylesByName = new Dictionary<string, SymbolStyle>();
                    ArcGISPortal portal = await ArcGISPortal.CreateAsync();

                    PortalItem styleItem = await PortalItem.CreateAsync(portal, "220936cc6ed342c9937abd8f180e7d1e");
                    var style = await SymbolStyle.OpenAsync(styleItem);
                    Symbol symbol;

                    switch (classValue)
                    {
                        case 1:
                            symbol = await style.GetSymbolAsync(new[] { "airport" });
                            break;
                        case 2:
                            symbol = await style.GetSymbolAsync(new[] { "amusement-park" });
                            break;
                        case 3:
                            symbol = await style.GetSymbolAsync(new[] { "aquarium" });
                            break;
                        case 4:
                            symbol = await style.GetSymbolAsync(new[] { "restaurant" });
                            break;
                        case 5:
                            symbol = await style.GetSymbolAsync(new[] { "hotel" });
                            break;
                        default:
                            symbol = await style.GetSymbolAsync(new[] { "pharmacy" });
                            break;
                    }

                    if (symbol is MultilayerPointSymbol multilayerPointSymbol)
                    {
                        multilayerPointSymbol.Size = attValue;
                        graphic.Symbol = multilayerPointSymbol;
                    }
                    else
                    {
                        Debug.WriteLine("Symbol is not a MultilayerPointSymbol.");
                    }
                }
                else if (symbolType == "modelLODs")
                {
                    MultilayerPointSymbol modelSymbollow = await createModelLayerFromFile("spider_low");
                    MultilayerPointSymbol modelSymbolmedium = await createModelLayerFromFile("spider_medium");
                    MultilayerPointSymbol modelSymbolhigh = await createModelLayerFromFile("spider_high");

                    graphicsOverlay.Graphics[0].Symbol = modelSymbollow;
                    graphicsOverlay.Graphics[1].Symbol = modelSymbolmedium;
                    graphicsOverlay.Graphics[2].Symbol = modelSymbolhigh;
                }
            }
        }

        private async Task StartTestLocationAsync(CancellationToken token)
        {
            if (_useSimulatedLocation)
            {
                sceneView.GeoViewDoubleTapped += OnSceneViewDoubleTapped_MoveLocation;
                Debug.WriteLine("Double tap screen to move simulated location");
                return;
            }

            #if ANDROID
            var status = await Permissions.CheckStatusAsync<
                Permissions.LocationWhenInUse>();

            if (status != PermissionStatus.Granted)
            {
                status = await Permissions.RequestAsync<
                    Permissions.LocationWhenInUse>();
            }

            token.ThrowIfCancellationRequested();

            if (status != PermissionStatus.Granted)
            {
                throw new UnauthorizedAccessException(
                    "Location permission is required. Enable it in Android Settings.");
            }
            #endif

            _locationSource.LocationChanged += OnLocationChanged;
            await _locationSource.StartAsync();

            token.ThrowIfCancellationRequested();
        }

        private void OnLocationChanged(object? sender, ArcLocation location)
        {
            CancellationTokenSource? workflow = _workflowCancellation;

            MainThread.BeginInvokeOnMainThread(() =>
            {
                if (workflow is null ||
                    !ReferenceEquals(workflow, _workflowCancellation) ||
                    workflow.IsCancellationRequested ||
                    _useSimulatedLocation ||
                    (passiveMode != "createTrack" &&
                    passiveMode != "traverseRoute"))
                {
                    return;
                }

                UpdateTestPosition(location.Position);
            });
        }

        private void CheckRouteStops(MapPoint position)
        {
            foreach (Graphic stop in _routeStops)
            {
                if (stop.Attributes["visited"] is true)
                {
                    continue;
                }

                if (stop.Geometry is not MapPoint stopPosition)
                {
                    throw new InvalidOperationException("Route stops must have point geometry");
                }

                MapPoint stopPositionWgs84 = (MapPoint)GeometryEngine.Project(stopPosition, SpatialReferences.Wgs84);

                var distance = GeometryEngine.DistanceGeodetic(
                    position, 
                    stopPositionWgs84,
                    LinearUnits.Meters,
                    AngularUnits.Degrees,
                    GeodeticCurveType.Geodesic);
                
                if (distance.Distance <= TriggerDistanceMeters)
                {
                    stop.Attributes["visited"] = true;
                    stop.Symbol = new SimpleMarkerSymbol(SimpleMarkerSymbolStyle.Circle, System.Drawing.Color.LimeGreen, 18);
                }
            }
        }

        private void ExportGraphics()
        {
            Debug.WriteLine("Exporting graphics...");
            foreach (var graphic in graphicsOverlay.Graphics)
            {
                Debug.WriteLine($"Graphic: {graphic.Geometry}");
            }
        }

        private void createMissileTrajectory()
        {
            Debug.WriteLine("Creating missile trajectory...");

            if (!(graphicsOverlay.Graphics.Count == 2))
            {
                Debug.WriteLine("Incorrect number of graphics for missile trajectory.");
                return;
            }

            Graphic startGraphic = graphicsOverlay.Graphics[0];
            Graphic endGraphic = graphicsOverlay.Graphics[1];

            MapPoint startPoint = (MapPoint)startGraphic.Geometry;
            MapPoint endPoint = (MapPoint)endGraphic.Geometry;

            GraphicsOverlay trajectoryOverlay = new GraphicsOverlay();
            SimpleRenderer simpleRenderer = new SimpleRenderer(new SimpleMarkerSymbol(SimpleMarkerSymbolStyle.Triangle, System.Drawing.Color.Red, 2));
            trajectoryOverlay.Renderer = simpleRenderer;

            for (int i = 0; i < 1000; i ++)
            {
                double dx = startPoint.X - endPoint.X;
                double dy = startPoint.Y - endPoint.Y;
                double dz = startPoint.Z - endPoint.Z;

                MapPoint trajectoryPoint = new MapPoint(
                    startPoint.X - dx * i / 1000,
                    startPoint.Y - dy * i / 1000,
                    startPoint.Z - dz * i / 1000,
                    startPoint.SpatialReference
                );
                Graphic trajectoryGraphic = new Graphic(trajectoryPoint, new SimpleMarkerSymbol(SimpleMarkerSymbolStyle.Circle, System.Drawing.Color.Red, 2));
                trajectoryOverlay.Graphics.Add(trajectoryGraphic);
            }

            sceneView.GraphicsOverlays.Add(trajectoryOverlay);
        }

        private async Task ReadGeoJsonPoints(string json, GraphicsOverlay? targetOverlay = null)
        {
            GraphicsOverlay destinationOverlay = targetOverlay ?? graphicsOverlay;

            using var stream = await FileSystem.OpenAppPackageFileAsync(json);
            using var reader = new StreamReader(stream);

            string fileContent = await reader.ReadToEndAsync();

            using var document = JsonDocument.Parse(fileContent);
            var root = document.RootElement;

            if (root.GetProperty("type").GetString() != "FeatureCollection")
                throw new NotSupportedException("Expected a GeoJSON FeatureCollection");

            var graphics = new List<Graphic>();

            foreach (var feature in root.GetProperty("features").EnumerateArray())
            {
                var geometry = feature.GetProperty("geometry");
            
                if (geometry.ValueKind == JsonValueKind.Null || geometry.GetProperty("type").GetString() != "Point")
                {
                    throw new NotSupportedException("Encountered non-Point geometry");
                }

                var coordinates = geometry.GetProperty("coordinates");

                var point = coordinates.GetArrayLength() > 2
                    ? new MapPoint(
                        coordinates[0].GetDouble(),
                        coordinates[1].GetDouble(),
                        coordinates[2].GetDouble(),
                        SpatialReferences.WebMercator
                    )
                    : new MapPoint(
                        coordinates[0].GetDouble(),
                        coordinates[1].GetDouble(),
                        SpatialReferences.WebMercator
                    );

                Graphic graphic = new Graphic(point);

                if (feature.TryGetProperty("properties", out var properties) &&
                    properties.ValueKind == JsonValueKind.Object)
                {
                    foreach (var property in properties.EnumerateObject())
                    {
                        graphic.Attributes[property.Name] = property.Value.ToString();
                    }
                }

                graphics.Add(graphic);
            }

            foreach(var graphic in graphics)
            {
                destinationOverlay.Graphics.Add(graphic);
            }
        }

        private async Task ReadJsonPoints(string json)
        {
            using var stream = await FileSystem.OpenAppPackageFileAsync(json);
            using var reader = new StreamReader(stream);

            string fileContent = await reader.ReadToEndAsync();

            using var document = JsonDocument.Parse(fileContent);
            var root = document.RootElement;

            var graphics = new List<Graphic>();

            foreach (var feature in root.GetProperty("features").EnumerateArray())
            {
                var x = feature.GetProperty("geometry").GetProperty("x").GetDouble();
                var y = feature.GetProperty("geometry").GetProperty("y").GetDouble();

                var point = new MapPoint(x, y, SpatialReferences.WebMercator);

                Graphic graphic = new Graphic(point);

                if (feature.TryGetProperty("attributes", out var attributes) &&
                    attributes.ValueKind == JsonValueKind.Object)
                {
                    foreach (var attribute in attributes.EnumerateObject())
                    {
                        graphic.Attributes[attribute.Name] = attribute.Value.ToString();
                    }
                }
                graphics.Add(graphic);
            }
            foreach(var graphic in graphics)
            {
                graphicsOverlay.Graphics.Add(graphic);
            }
        }

        private async Task ImportGraphicsFromServiceAsync(string serviceUrl, string whereClause = "1=1")
        {
            try
            {
                Uri uri = new Uri(serviceUrl);

                var table = new ServiceFeatureTable(uri);
                await table.LoadAsync();

                if (whereClause == "extent")
                {
                    var extent = sceneView.GetCurrentViewpoint(ViewpointType.BoundingGeometry)?.TargetGeometry?? throw new InvalidOperationException("Scene extent unavailable");

                    var query = new QueryParameters
                    {
                        WhereClause = "1=1",
                        MaxFeatures = 500,
                        Geometry = extent,
                        SpatialRelationship = SpatialRelationship.Intersects
                    };

                    var result = await table.QueryFeaturesAsync(query, QueryFeatureFields.LoadAll);

                    foreach (var feature in result)
                    {
                        SimpleMarkerSymbol symbol = new SimpleMarkerSymbol(SimpleMarkerSymbolStyle.X, System.Drawing.Color.Black, 10);
                        graphicsOverlay.Graphics.Add(new Graphic(feature.Geometry, feature.Attributes, symbol));
                    }
                }
                else
                {
                    var query = new QueryParameters
                    {
                        WhereClause = whereClause,
                        MaxFeatures = 500
                    };

                    var result = await table.QueryFeaturesAsync(query, QueryFeatureFields.LoadAll);

                    foreach (var feature in result)
                    {
                        graphicsOverlay.Graphics.Add(new Graphic(feature.Geometry, feature.Attributes));
                    }     
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }
        }
        private async Task<string> GetAddressAsync(MapPoint point)
        {
                var locator = await LocatorTask.CreateAsync(new Uri(
                    "https://geocode-api.arcgis.com/arcgis/rest/services/World/GeocodeServer"));

                var results = await locator.ReverseGeocodeAsync(
                    point,
                    new ReverseGeocodeParameters
                    {
                        MaxResults = 1
                    });

                return results.FirstOrDefault()?.Label ?? "No address found";
        }

        private async Task TransferOverlayToMap()
        {
            var mapView = new MapView
            {
                Map = new Esri.ArcGISRuntime.Mapping.Map(BasemapStyle.ArcGISImagery)
            };

            sceneView.GraphicsOverlays.Remove(graphicsOverlay);
            mapView.GraphicsOverlays.Add(graphicsOverlay);

            var layout = new Microsoft.Maui.Controls.Grid
            {
                RowDefinitions =
                {
                    new RowDefinition {Height = GridLength.Auto},
                    new RowDefinition {Height = GridLength.Star}
                }
            };

            layout.Add(mapView, 0, 1);

            var page = new ContentPage
            {
                Title = "Map view",
                Content = layout
            };

            await Navigation.PushAsync(page);
        }

        private async Task ShowNearbyPoiAsync(MapPoint location, GraphicsOverlay targetOverlay, CancellationToken cancellationToken)
        {
            var query = string.Format(
                CultureInfo.InvariantCulture,
                "[out:json][timeout:20];nwr(around:{0},{1},{2})[\"shop\"=\"convenience\"];out center 40;",
                NearbyRadiusMeters, location.Y, location.X);

            try
            {
                using var request = new HttpRequestMessage(
                    HttpMethod.Post,
                    "https://overpass.private.coffee/api/interpreter")
                {
                    Content = new StringContent(
                        $"data={Uri.EscapeDataString(query)}",
                        Encoding.UTF8,
                        "application/x-www-form-urlencoded")
                };
                request.Headers.UserAgent.ParseAdd("NETGraphicsTester/1.0");

                using HttpResponseMessage response = await OverpassHttpClient.SendAsync(request, cancellationToken);
                response.EnsureSuccessStatusCode();

                string responseJson = await response.Content.ReadAsStringAsync(cancellationToken);
                using JsonDocument document = JsonDocument.Parse(responseJson);

                int addedCount = 0;
                foreach (JsonElement element in document.RootElement.GetProperty("elements").EnumerateArray())
                {
                    if (!TryGetOverpassCoordinates(element, out double longitude, out double latitude))
                    {
                        continue;
                    }

                    MapPoint poiLocation = (MapPoint)GeometryEngine.Project(
                        new MapPoint(longitude, latitude, SpatialReferences.Wgs84),
                        location.SpatialReference);

                    string name = "Convenience store";
                    if (element.TryGetProperty("tags", out JsonElement tags) &&
                        tags.TryGetProperty("name", out JsonElement nameElement))
                    {
                        name = nameElement.ToString();
                    }

                    cancellationToken.ThrowIfCancellationRequested();

                    var poiGraphic = new Graphic(
                        poiLocation,
                        new SimpleMarkerSymbol(SimpleMarkerSymbolStyle.Diamond, System.Drawing.Color.LimeGreen, 14));
                    poiGraphic.Attributes["name"] = name;
                    poiGraphic.Attributes["source"] = "OpenStreetMap";
                    targetOverlay.Graphics.Add(poiGraphic);
                    addedCount++;
                }

                if (addedCount == 0)
                {
                    await DisplayAlert("Nearby points", "No convenience stores were found within 800 meters.", "OK");
                }
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Unable to query nearby convenience stores: {ex}");
                await DisplayAlert("Nearby points", $"The Overpass query failed: {ex.Message}", "OK");
            }
        }

        private static bool TryGetOverpassCoordinates(JsonElement element, out double longitude, out double latitude)
        {
            if (element.TryGetProperty("lon", out JsonElement lonElement) &&
                element.TryGetProperty("lat", out JsonElement latElement) &&
                lonElement.TryGetDouble(out longitude) &&
                latElement.TryGetDouble(out latitude))
            {
                return true;
            }

            if (element.TryGetProperty("center", out JsonElement center) &&
                center.TryGetProperty("lon", out lonElement) &&
                center.TryGetProperty("lat", out latElement) &&
                lonElement.TryGetDouble(out longitude) &&
                latElement.TryGetDouble(out latitude))
            {
                return true;
            }

            longitude = 0;
            latitude = 0;
            return false;
        }

        private async Task AnimateDroneDestinationsAsync(Graphic movingGraphic, CancellationTokenSource workflowCancellation)
        {
            _isDroneAnimating = true;
            CancellationToken cancellationToken = workflowCancellation.Token;

            try
            {
                while (_droneDestinations.Count > 0)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    MapPoint destination = _droneDestinations.Dequeue();
                    MapPoint start = (MapPoint)GeometryEngine.Project(
                        movingGraphic.Geometry,
                        SpatialReferences.WebMercator);

                    for (int step = 1; step <= 100; step++)
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        double progress = step / 100d;
                        movingGraphic.Geometry = new MapPoint(
                            start.X + (destination.X - start.X) * progress,
                            start.Y + (destination.Y - start.Y) * progress,
                            SpatialReferences.WebMercator);

                        await Task.Delay(10, cancellationToken);
                    }
                }
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
            }
            finally
            {
                if (ReferenceEquals(_workflowCancellation, workflowCancellation))
                {
                    _isDroneAnimating = false;
                }
            }
        }

        private async Task<Dictionary<long, PointSnapShot>> ReadPointSnapshotAsync(CancellationToken token)
        {
            const string serviceUrl = "https://services5.arcgis.com/N82JbI5EYtAkuUKU/arcgis/rest/services/SF_Points_3D/FeatureServer/12";

            var table = new ServiceFeatureTable(new Uri(serviceUrl));
            await table.LoadAsync();
            token.ThrowIfCancellationRequested();

            var result = await table.QueryFeaturesAsync(
                new QueryParameters { WhereClause = "1=1" },
                QueryFeatureFields.LoadAll
            );

            token.ThrowIfCancellationRequested();

            if (result.IsTransferLimitExceeded)
            {
                throw new InvalidOperationException("The snapshot is incomplete. Add pagination before comparing deletions.");
            }

            var snapshot = new Dictionary<long, PointSnapShot>();
            foreach (Feature feature in result)
            {
                if (feature.Geometry is not MapPoint point)
                {
                    throw new InvalidOperationException("This workflow requires point features.");
                }

                long id = Convert.ToInt64(feature.Attributes[table.ObjectIdField], CultureInfo.InvariantCulture);

                var attributes = feature.Attributes.ToDictionary(
                    pair => pair.Key, pair => pair.Value);
                
                string signature = JsonSerializer.Serialize(new
                {
                    Geometry = point.ToJson(),
                    Attributes = attributes.OrderBy(pair => pair.Key, StringComparer.Ordinal).ToArray()
                });

                snapshot.Add(id, new PointSnapShot(
                    point,
                    attributes,
                    signature));
            }

            return snapshot;
        }

        private async Task FlashPointsAsync(
            GraphicsOverlay overlay,
            IEnumerable<MapPoint> points,
            CancellationToken token)
        {
            var flashes = points.Select(point => new Graphic(
                point,
                new SimpleMarkerSymbol(SimpleMarkerSymbolStyle.Circle, System.Drawing.Color.Magenta, 30)
            )).ToList();

            foreach (Graphic flash in flashes)
            {
                overlay.Graphics.Add(flash);
            }

            try
            {
                for (int i = 0; i < 6; i++)
                {
                    token.ThrowIfCancellationRequested();

                    foreach (Graphic flash in flashes)
                    {
                        flash.IsVisible = i % 2 == 0;
                    }
                    await Task.Delay(200, token);
                }
            }
            finally
            {
                foreach (Graphic flash in flashes)
                {
                    overlay.Graphics.Remove(flash);
                }
            }
        }

        private async Task AnimateSatelliteOrbitAsync(GraphicsOverlay overlay, CancellationToken token)
        {
            const double altitudeMeters = 500_000;
            const double secondsPerRevolution = 60;
            
            var colors = new[]
            {
                System.Drawing.Color.Cyan,
                System.Drawing.Color.Magenta,
                System.Drawing.Color.Yellow,
            };

            MapPoint GetPosition(int index, double elapsedSeconds)
            {
                double progress = elapsedSeconds / secondsPerRevolution;
                double longitude = -122.4194 + (index - 1) * 4 + progress * 360;

                longitude = ((longitude + 180) % 360 + 360) % 360 - 180;

                double latitude = 37.7749 + (index - 1) * 2 + 12 * Math.Sin(progress * 2 * Math.PI);

                return new MapPoint(longitude, latitude, altitudeMeters, SpatialReferences.Wgs84);
            }

            var satellites = new Graphic[colors.Length];

            for (int i = 0; i < colors.Length; i++)
            {
                token.ThrowIfCancellationRequested();

                var satellite = new Graphic(
                    GetPosition(i, 0),
                    new SimpleMarkerSymbol(SimpleMarkerSymbolStyle.Circle, colors[i], 32)
                );

                satellite.Attributes["satellite_id"] = i + 1;

                satellites[i] = satellite;
                overlay.Graphics.Add(satellite);
            }

            var stopwatch = Stopwatch.StartNew();
            using var timer = new PeriodicTimer(TimeSpan.FromMilliseconds(33));

            while (await timer.WaitForNextTickAsync(token))
            {
                token.ThrowIfCancellationRequested();

                double elapsedSeconds = stopwatch.Elapsed.TotalSeconds;

                for (int i = 0; i < satellites.Length; i++)
                {
                    satellites[i].Geometry = GetPosition(i, elapsedSeconds);
                }
            }
        }

        private async Task AnimateAircraftAsync(GraphicsOverlay overlay, MapPoint[] points, CancellationToken token)
        {
            const int aircraftCount = 3;
            const double altitudeMeters = 1000;
            const double secondsPerLeg = 3;

            var aircraft = new Graphic[aircraftCount];
            var startIndices = new int[aircraftCount];
            var destinationIndices = new int[aircraftCount];

            MapPoint AtAltitude(MapPoint point) => 
                new MapPoint(point.X, point.Y, altitudeMeters, SpatialReferences.WebMercator);
            
            for (int i = 0; i < aircraftCount; i ++)
            {
                token.ThrowIfCancellationRequested();

                startIndices[i] = random.Next(points.Length);

                var graphic = new Graphic(AtAltitude(points[startIndices[i]]));
                graphic.Attributes["aircraft_id"] = i + 1;
                graphic.Attributes["rotation_value"] = 0d;

                aircraft[i] = graphic;
                overlay.Graphics.Add(graphic);
            }

            var stopwatch = new Stopwatch();
            using var timer = new PeriodicTimer(TimeSpan.FromMilliseconds(33));

            while (true)
            {
                token.ThrowIfCancellationRequested();

                for (int i = 0; i < aircraftCount; i++)
                {
                    int destinationIndex = random.Next(points.Length - 1);
                    if (destinationIndex >= startIndices[i])
                    {
                        destinationIndex++;
                    }

                    destinationIndices[i] = destinationIndex;

                    MapPoint start = points[startIndices[i]];
                    MapPoint destination = points[destinationIndex];

                    double heading = Math.Atan2(destination.X - start.X, destination.Y - start.Y) * 180 / Math.PI;

                    aircraft[i].Attributes["rotation_value"] = (heading + 360) % 360;
                }

                stopwatch.Restart();
                double progress;

                do
                {
                    if (!await timer.WaitForNextTickAsync(token))
                    {
                        return;
                    }

                    token.ThrowIfCancellationRequested();

                    progress = Math.Min(stopwatch.Elapsed.TotalSeconds / secondsPerLeg, 1);

                    for (int i = 0; i < aircraftCount; i++)
                    {
                        MapPoint start = points[startIndices[i]];
                        MapPoint destination = points[destinationIndices[i]];

                        aircraft[i].Geometry = new MapPoint(
                            start.X + (destination.X - start.X) * progress,
                            start.Y + (destination.Y - start.Y) * progress,
                            altitudeMeters,
                            SpatialReferences.WebMercator);
                    }
                }
                while (progress < 1);

                Array.Copy(destinationIndices, startIndices, aircraftCount);
            }
        }

        private async Task AnimateBusesAsync(
            GraphicsOverlay overlay,
            (MapPoint Point, string Id)[] stops,
            CancellationToken token)
        {
            const int busCount = 3;
            const double durationSeconds = 60;
            const double proximityMeters = 300;
            const double heightAboveGroundMeters = 5;

            double minX = stops.Min(stop => stop.Point.X);
            double maxX = stops.Max(stop => stop.Point.X);
            double minY = stops.Min(stop => stop.Point.Y);
            double maxY = stops.Max(stop => stop.Point.Y);

            double padding = Math.Max((maxX - minX) * 0.1, 500);
            double startX = minX - padding;
            double endX = maxX + padding;

            var colors = new[]
            {
                System.Drawing.Color.Cyan,
                System.Drawing.Color.Orange,
                System.Drawing.Color.Magenta
            };

            var buses = new Graphic[busCount];
            var labels = new Graphic[busCount];
            var textSymbols = new TextSymbol[busCount];
            var routeY = new double[busCount];

            for (int i = 0; i < busCount; i++)
            {
                token.ThrowIfCancellationRequested();

                routeY[i] = minY + (maxY - minY) * (i + 1d) / (busCount + 1);

                var start = new MapPoint(
                    startX,
                    routeY[i],
                    heightAboveGroundMeters,
                    SpatialReferences.WebMercator);

                var bus = new Graphic(
                    start,
                    new SimpleMarkerSymbol(
                        SimpleMarkerSymbolStyle.Square,
                        colors[i],
                        20));

                bus.Attributes["bus_id"] = i + 1;

                var text = new TextSymbol(
                    string.Empty,
                    System.Drawing.Color.Black,
                    14,
                    Esri.ArcGISRuntime.Symbology.HorizontalAlignment.Center,
                    Esri.ArcGISRuntime.Symbology.VerticalAlignment.Bottom)
                {
                    OffsetY = 20,
                    HaloColor = System.Drawing.Color.White,
                    HaloWidth = 2
                };

                var label = new Graphic(start, text)
                {
                    IsVisible = false
                };

                buses[i] = bus;
                labels[i] = label;
                textSymbols[i] = text;

                overlay.Graphics.Add(bus);
                overlay.Graphics.Add(label);
            }

            void UpdatePositions(double progress)
            {
                for (int i = 0; i < busCount; i++)
                {
                    var position = new MapPoint(
                        startX + (endX - startX) * progress,
                        routeY[i],
                        heightAboveGroundMeters,
                        SpatialReferences.WebMercator);

                    buses[i].Geometry = position;
                    labels[i].Geometry = position;

                    int nearestStopIndex = -1;
                    double nearestDistance = proximityMeters;

                    for (int j = 0; j < stops.Length; j++)
                    {
                        double distance = GeometryEngine.DistanceGeodetic(
                            position,
                            stops[j].Point,
                            LinearUnits.Meters,
                            AngularUnits.Degrees,
                            GeodeticCurveType.Geodesic).Distance;

                        if (distance <= nearestDistance)
                        {
                            nearestDistance = distance;
                            nearestStopIndex = j;
                        }
                    }

                    if (nearestStopIndex >= 0)
                    {
                        textSymbols[i].Text =
                            $"Bus {i + 1}: nearing stop {stops[nearestStopIndex].Id}";
                    }

                    labels[i].IsVisible = nearestStopIndex >= 0;
                }
            }

            UpdatePositions(0);

            var stopwatch = Stopwatch.StartNew();
            using var timer = new PeriodicTimer(TimeSpan.FromMilliseconds(33));

            while (await timer.WaitForNextTickAsync(token))
            {
                token.ThrowIfCancellationRequested();

                double progress = Math.Min(
                    stopwatch.Elapsed.TotalSeconds / durationSeconds, 1);

                UpdatePositions(progress);

                if (progress >= 1)
                {
                    break;
                }
            }
        }
    }
}