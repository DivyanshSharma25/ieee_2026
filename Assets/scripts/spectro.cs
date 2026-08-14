using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using Autohand;

public class spectro : MonoBehaviour
{
    [Header("Hinge Settings")]
    public Transform hinge;
    public float openAngleX = 0f;
    public float closedAngleX = -90f;
    public float rotationSpeed = 3f;

    [Header("Sample Point")]
    public PlacePoint samplePoint;
    public float measurementDelay = 2f;

    [Header("Calibration")]
    [Tooltip("Slope m in the straight-line equation A = m * concentration + c")]
    public float calibrationSlope = 1f;
    [Tooltip("Intercept c in the straight-line equation A = m * concentration + c")]
    public float calibrationIntercept = 0f;
    public string waterName = "Water";

    [Header("Display")]
    public TMP_Text displayText;

    [Header("Graph")]
    public SpectroGraphDisplay graphDisplay;
    public List<Vector2> measurementHistory = new List<Vector2>();

    private bool isOpen = true;
    private bool isLocked = false;
    private bool pendingMeasurement = false;
    private Coroutine measurementRoutine;

    private void Start()
    {
        if (displayText == null)
            displayText = GetComponentInChildren<TMP_Text>();

        if (displayText != null)
            displayText.text = "Ready";
    }

    private void Update()
    {
        if (hinge == null)
            return;

        if (isLocked)
            isOpen = false;

        float targetX = isOpen ? openAngleX : closedAngleX;
        Vector3 currentEuler = hinge.localEulerAngles;
        float newX = Mathf.LerpAngle(currentEuler.x, targetX, Time.deltaTime * rotationSpeed);
        hinge.localEulerAngles = new Vector3(newX, currentEuler.y, currentEuler.z);

        if (pendingMeasurement && !isOpen)
        {
            float closedDelta = Mathf.Abs(Mathf.DeltaAngle(newX, closedAngleX));
            if (closedDelta < 0.25f)
            {
                pendingMeasurement = false;
                StartMeasurement();
            }
        }
    }

    public void OnButtonPressed()
    {
        if (isLocked)
        {
            print("Spectro is measuring. Please wait.");
            return;
        }

        if (isOpen)
        {
            CloseMeter();
            pendingMeasurement = true;
            return;
        }

        pendingMeasurement = false;
        OpenMeter();
    }

    public void OpenMeter()
    {
        if (isLocked)
            return;

        isOpen = true;
        print("Spectro opened");
    }

    public void CloseMeter()
    {
        if (isLocked)
            return;

        isOpen = false;
        print("Spectro closed");
    }

    public void StartMeasurement()
    {
        if (isLocked)
            return;

        if (isOpen)
        {
            pendingMeasurement = true;
            print("Spectro lid is open. Closing before measuring...");
            CloseMeter();
            return;
        }

        if (samplePoint == null || samplePoint.placedObject == null)
        {
            if (displayText != null)
                displayText.text = "No sample";

            print("No object placed in sample point");
            return;
        }

        var placedObject = samplePoint.placedObject.gameObject;
        var container = placedObject.GetComponentInParent<LiquidContainer>();

        if (container == null)
        {
            if (displayText != null)
                displayText.text = "No container";

            print("Placed object does not contain a LiquidContainer");
            return;
        }

        if (measurementRoutine != null)
            StopCoroutine(measurementRoutine);

        measurementRoutine = StartCoroutine(MeasureRoutine(container));
    }

    private IEnumerator MeasureRoutine(LiquidContainer container)
    {
        isLocked = true;
        isOpen = false;

        if (displayText != null)
            displayText.text = "Measuring...";

        print("Measuring sample...");

        yield return new WaitForSeconds(measurementDelay);

        var data = container.CurrentFluidData;
        float concentration = GetNonWaterConcentration(data);
        float absorbance = CalculateAbsorbance(concentration);

        measurementHistory.Add(new Vector2(concentration, absorbance));
        if (graphDisplay != null)
            graphDisplay.UpdateGraph(measurementHistory, calibrationSlope, calibrationIntercept);

        if (displayText != null)
            displayText.text = "Absorbance: " + absorbance.ToString("F3");

        print("Non-water concentration: " + concentration + " | Absorbance: " + absorbance);

        isLocked = false;
        measurementRoutine = null;
    }

    public void SetCalibration(float slope, float intercept)
    {
        calibrationSlope = slope;
        calibrationIntercept = intercept;
    }

    private float GetNonWaterConcentration(FluidData data)
    {
        if (data.mixture == null || data.mixture.Count == 0)
            return 0f;

        float nonWaterConcentration = 0f;

        foreach (var component in data.mixture)
        {
            if (component == null)
                continue;

            if (IsWater(component.fluidName))
                continue;

            nonWaterConcentration += Mathf.Clamp01(component.concentration);
        }

        return Mathf.Clamp01(nonWaterConcentration);
    }

    private bool IsWater(string fluidName)
    {
        if (string.IsNullOrWhiteSpace(fluidName))
            return false;

        return string.Equals(fluidName.Trim(), waterName.Trim(), System.StringComparison.OrdinalIgnoreCase);
    }

    private float CalculateAbsorbance(float nonWaterConcentration)
    {
        // Linear calibration curve: A = m * C + c
        return calibrationSlope * nonWaterConcentration + calibrationIntercept;
    }

    public void OnMeasureButtonPressed()
    {
        if (isOpen)
        {
            pendingMeasurement = true;
            CloseMeter();
            return;
        }

        StartMeasurement();
    }

    public void OnResetButtonPressed()
    {
        if (displayText != null)
            displayText.text = "Ready";

        if (measurementRoutine != null)
        {
            StopCoroutine(measurementRoutine);
            measurementRoutine = null;
        }

        pendingMeasurement = false;
        isLocked = false;
        isOpen = true;
        print("Spectro reset");
    }
}