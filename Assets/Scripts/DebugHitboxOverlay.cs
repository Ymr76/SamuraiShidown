using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// Draws hit boxes (red, the damage area) and hurt boxes (green, where a character can be hit)
/// directly in the running game view. Toggle it on/off from the Settings panel.
///
/// Uses the Scriptable Render Pipeline (URP) end-of-camera callback and draws in screen
/// space, which renders reliably in URP (legacy OnRenderObject GL drawing does not).
/// </summary>
[DefaultExecutionOrder(1000)]
public class DebugHitboxOverlay : MonoBehaviour
{
    // Static so the Settings toggle can flip it without holding a direct reference.
    public static bool Enabled = false;

    [Header("Colors")]
    public Color hurtboxColor = new Color(0f, 1f, 0.2f, 1f);  // green = where you can be hit (collider)
    public Color hitboxColor = new Color(1f, 0.15f, 0.1f, 1f); // red  = the strike / damage area

    private Material lineMaterial;

    private void EnsureMaterial()
    {
        if (lineMaterial != null) return;
        Shader shader = Shader.Find("Hidden/Internal-Colored");
        lineMaterial = new Material(shader) { hideFlags = HideFlags.HideAndDontSave };
        lineMaterial.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha);
        lineMaterial.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);
        lineMaterial.SetInt("_Cull", (int)CullMode.Off);
        lineMaterial.SetInt("_ZWrite", 0);
        lineMaterial.SetInt("_ZTest", (int)CompareFunction.Always);
    }

    // Drawn via IMGUI so it renders on top of the game view independently of the
    // active render pipeline (works reliably in URP and is captured in screenshots).
    private void OnGUI()
    {
        if (!Enabled) return;
        if (Event.current == null || Event.current.type != EventType.Repaint) return;

        var cam = Camera.main;
        if (cam == null) return;
        _cam = cam;

        EnsureMaterial();

        GL.PushMatrix();
        lineMaterial.SetPass(0);
        GL.LoadPixelMatrix(); // pixel coordinates, origin bottom-left (matches WorldToScreenPoint)

        // Player: hurtbox (collider) + hit box (attack circle)
        var player = Object.FindFirstObjectByType<PlayerMovement>();
        if (player != null)
        {
            var col = player.GetComponent<Collider2D>();
            if (col != null) DrawRect(col.bounds, hurtboxColor);
            if (player.attackPoint != null)
                DrawCircle(player.attackPoint.position, player.attackRange, hitboxColor);
        }

        // Enemies: hurtbox (collider) + hit box (strike range circle matching ExecuteHit)
        foreach (var enemy in Object.FindObjectsByType<Enemy>(FindObjectsSortMode.None))
        {
            if (enemy == null) continue;
            var col = enemy.GetComponent<Collider2D>();
            if (col != null) DrawRect(col.bounds, hurtboxColor);
            DrawCircle(enemy.transform.position, enemy.hitRange, hitboxColor);
        }

        GL.PopMatrix();
    }

    private Camera _cam;

    private Vector3 ToScreen(Vector3 world)
    {
        // WorldToScreenPoint origin is bottom-left; OnGUI / GL.LoadPixelMatrix here is
        // top-left, so flip Y to land the boxes on the characters.
        Vector3 s = _cam.WorldToScreenPoint(world);
        return new Vector3(s.x, Screen.height - s.y, 0f);
    }

    private void DrawRect(Bounds b, Color c)
    {
        GL.Begin(GL.LINES);
        GL.Color(c);
        Vector3 p0 = ToScreen(new Vector3(b.min.x, b.min.y, 0f));
        Vector3 p1 = ToScreen(new Vector3(b.max.x, b.min.y, 0f));
        Vector3 p2 = ToScreen(new Vector3(b.max.x, b.max.y, 0f));
        Vector3 p3 = ToScreen(new Vector3(b.min.x, b.max.y, 0f));
        Line(p0, p1); Line(p1, p2); Line(p2, p3); Line(p3, p0);
        GL.End();
    }

    private void DrawCircle(Vector3 center, float radius, Color c)
    {
        const int seg = 32;
        GL.Begin(GL.LINES);
        GL.Color(c);
        Vector3 prev = ToScreen(center + new Vector3(radius, 0f, 0f));
        for (int i = 1; i <= seg; i++)
        {
            float a = (i / (float)seg) * Mathf.PI * 2f;
            Vector3 cur = ToScreen(center + new Vector3(Mathf.Cos(a) * radius, Mathf.Sin(a) * radius, 0f));
            Line(prev, cur);
            prev = cur;
        }
        GL.End();
    }

    private void Line(Vector3 a, Vector3 b)
    {
        GL.Vertex(a);
        GL.Vertex(b);
    }
}
