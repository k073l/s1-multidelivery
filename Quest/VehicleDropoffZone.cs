using DeliveryProject.Builders;
using DeliveryProject.Helpers;
using DeliveryProject.Persistence;
using DeliveryProject.Pool;
using MelonLoader;
using UnityEngine;
#if MONO
using ScheduleOne.Delivery;
using ScheduleOne.DevUtilities;
using ScheduleOne.Vehicles;
#else
using Il2CppScheduleOne.Delivery;
using Il2CppScheduleOne.DevUtilities;
using Il2CppScheduleOne.Vehicles;
#endif


namespace DeliveryProject.Quest;

[RegisterTypeInIl2Cpp]
public class VehicleDropoffZone : MonoBehaviour
{
    private VehicleDetector _detector;
    private Logger _logger;
    private GameObject _visualPlane;
    private Material _visualMaterial;
    private Color _baseColor;

    private DropoffQuest? _quest;

    public Vector3 Corner1 { get; private set; }
    public Vector3 Corner2 { get; private set; }
    public bool ShowVisuals { get; set; } = true;

    private void Awake()
    {
        _logger = new Logger("VehicleDropoffZone");
    }

    private void Start()
    {
        _detector = gameObject.GetComponent<VehicleDetector>();
        if (_detector == null)
        {
            _detector = gameObject.AddComponent<VehicleDetector>();
        }
    }

    private void Update()
    {
        if (_detector.vehicles.Count > 0)
        {
            foreach (var vehicle in _detector.vehicles.AsEnumerable())
            {
                OnVehicleEntered(vehicle);
            }
        }
    }

    private void OnVehicleEntered(LandVehicle vehicle)
    {
        _logger.Debug($"Vehicle entered dropoff zone: {vehicle.vehicleName}");

        if (!IsCorrectVehicleType(vehicle))
        {
            if (Time.frameCount % 60 == 0) _logger.Warning($"Wrong vehicle type: {vehicle.vehicleCode}");
            return;
        }

        if (vehicle.IsOccupied)
        {
            _logger.Msg("Ejecting player from vehicle");
            vehicle.ExitVehicle();
        }

        ProcessAndAddToPool(vehicle);
        _detector.vehicles.Remove(vehicle);
        Destroy(gameObject);
    }

    private static bool IsCorrectVehicleType(LandVehicle vehicle)
    {
        return vehicle.vehicleCode == DeliveryProject.RequestedVehicleCode;
    }

    private void ProcessAndAddToPool(LandVehicle vehicle)
    {
        _logger.Debug($"Adding vehicle to pool: {vehicle.GUID}");

        var deliveryVehicle = vehicle.GetComponent<DeliveryVehicle>();
        if (deliveryVehicle == null)
        {
            var guid = vehicle.GUID;
            deliveryVehicle = new DeliveryVehicleBuilder()
                .WithLandVehicle(vehicle)
                .WithGuid(guid)
                .Build();
        }

        vehicle.IsPlayerOwned = false;
        vehicle.SetIsPlayerOwned(null, false);
        vehicle.SetVisible(false);
        vehicle.IsPhysicallySimulated = false;

        vehicle.transform.position = new Vector3(0f, -100f, 0f);

        PoolManager.Instance.AddToSaveData(deliveryVehicle);
        PoolManager.Instance.AddToPool(deliveryVehicle);
        if (_quest is { } dq) dq.MarkAddVehicleEntryComplete();
        _logger.Msg($"Vehicle added to pool. Total vehicles: {PoolManager.Instance.Pool.Count}");
    }

    public void SetupZone(Vector3 corner1, Vector3 corner2, float height = 5f, Color? visualColor = null)
    {
        Corner1 = corner1;
        Corner2 = corner2;

        var center = (corner1 + corner2) / 2f;
        var size = new Vector3(
            Mathf.Abs(corner2.x - corner1.x),
            height,
            Mathf.Abs(corner2.z - corner1.z)
        );

        transform.position = center;

        var boxCollider = gameObject.GetComponent<BoxCollider>();
        if (boxCollider == null)
        {
            boxCollider = gameObject.AddComponent<BoxCollider>();
        }

        boxCollider.isTrigger = true;
        boxCollider.size = size;
        boxCollider.center = Vector3.zero;

        if (ShowVisuals)
        {
            _baseColor = visualColor ?? new Color(0f, 1f, 1f, 0.3f);
            CreateVisualPlane(size, _baseColor);
        }

        _logger.Debug($"Dropoff zone created: Center={center}, Size={size}");
    }

    private void CreateVisualPlane(Vector3 size, Color color)
    {
        _visualPlane = GameObject.CreatePrimitive(PrimitiveType.Cube);
        _visualPlane.name = "DropoffZoneVisual";
        _visualPlane.transform.SetParent(transform);
        _visualPlane.transform.localPosition = Vector3.zero;
        _visualPlane.transform.localScale = new Vector3(size.x, 0.1f, size.z);

        var visualCollider = _visualPlane.GetComponent<Collider>();
        if (visualCollider != null)
        {
            Destroy(visualCollider);
        }

        ApplyDebugMaterial(_visualPlane, color);
    }

    private void ApplyDebugMaterial(GameObject obj, Color color)
    {
        var renderer = obj.GetComponent<Renderer>();
        if (renderer == null) return;

        var shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null) return;

        _visualMaterial = new Material(shader);

        if (_visualMaterial.HasProperty("_Surface"))
            _visualMaterial.SetFloat("_Surface", 1f);

        if (color.a <= 0f) color.a = 0.3f;

        if (_visualMaterial.HasProperty("_BaseColor"))
            _visualMaterial.SetColor("_BaseColor", color);

        if (_visualMaterial.HasProperty("_EmissionColor"))
        {
            _visualMaterial.EnableKeyword("_EMISSION");
            _visualMaterial.SetColor("_EmissionColor", new Color(color.r, color.g, color.b) * 1.5f);
        }

        _visualMaterial.SetInt("_ZWrite", 0);
        _visualMaterial.renderQueue = 3000;

        renderer.material = _visualMaterial;
    }

    public void SetVisualsEnabled(bool enabled)
    {
        ShowVisuals = enabled;
        if (_visualPlane != null)
        {
            _visualPlane.SetActive(enabled);
        }
    }

    public void SetQuest(DropoffQuest quest)
    {
        _quest = quest;
    }

    private void OnDestroy()
    {
        if (_visualPlane != null)
        {
            Destroy(_visualPlane);
        }
    }
}