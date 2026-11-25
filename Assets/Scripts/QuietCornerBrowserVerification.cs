using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

// Explicit opt-in runtime checks, using the same controller as the WebGL build.
public sealed class QuietCornerBrowserVerification : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void StartIfRequested()
    {
        if (System.Array.IndexOf(System.Environment.GetCommandLineArgs(), "-qc-browser-verify") >= 0)
            new GameObject("Browser input regression checks").AddComponent<QuietCornerBrowserVerification>();
    }
    readonly List<string> checks = new List<string>();
    int failures, errors;
    Mouse mouse;
    Keyboard keyboard;
    Chapter6XRPlayer rig;
    Vector2 Center => new Vector2(Screen.width * .5f, Screen.height * .5f);
    IEnumerator Frames(int count = 4) { for (int i = 0; i < count; i++) yield return null; }
    void Check(bool ok, string name) { checks.Add((ok ? "PASS: " : "FAIL: ") + name); if (!ok) failures++; }
    void Log(string message, string stack, LogType type) { if (type == LogType.Error || type == LogType.Exception) errors++; }
    IEnumerator Pointer(Vector2 delta, bool click = false)
    {
        var state = new MouseState { position = Center, delta = delta };
        if (click) state = state.WithButton(MouseButton.Left);
        InputSystem.QueueStateEvent(mouse, state);
        yield return Frames();
    }
    IEnumerator AimAt(QuietCornerTarget target)
    {
        Vector3 direction = target.GetComponentInChildren<Collider>().bounds.center - rig.head.transform.position;
        var desired = Quaternion.LookRotation(direction).eulerAngles;
        float yaw = Mathf.DeltaAngle(rig.transform.eulerAngles.y, desired.y);
        float pitch = Mathf.DeltaAngle(rig.head.transform.localEulerAngles.x, desired.x);
        yield return Pointer(new Vector2(yaw, -pitch) * Screen.height / 100);
    }
    IEnumerator Start()
    {
        // Pointer capture is a window-focus feature. Give the test operator a
        // chance to focus this standalone window before checking real capture.
        yield return new WaitForSecondsRealtime(10);
        Application.logMessageReceived += Log;
        Check(Application.isFocused, "Standalone test window has focus for pointer capture");
        rig = FindFirstObjectByType<Chapter6XRPlayer>();
        var experience = rig.experience;
        var browser = QuietCornerBrowserControls.EnablePreview();
        string[] args = System.Environment.GetCommandLineArgs();
        int outputIndex = System.Array.IndexOf(args, "-qc-browser-output");
        string output = outputIndex >= 0 ? args[outputIndex + 1] : Application.persistentDataPath;
        Directory.CreateDirectory(output);
        mouse = InputSystem.AddDevice<Mouse>("Mouse-look regression mouse");
        keyboard = InputSystem.AddDevice<Keyboard>("Mouse-look regression keyboard");
        mouse.MakeCurrent();
        keyboard.MakeCurrent();
        yield return Frames();
        Check(browser != null && !rig.enabled && !rig.simulator.enabled, "Direct camera is independent of simulated XR poses");
        yield return new WaitForEndOfFrame();
        var overview=ScreenCapture.CaptureScreenshotAsTexture();
        File.WriteAllBytes(Path.Combine(output,"Cafe_Overview.png"),overview.EncodeToPNG());
        Destroy(overview);
        var initial = rig.head.transform.rotation;
        yield return Pointer(new Vector2(120, 60));
        Check(Quaternion.Angle(initial, rig.head.transform.rotation) < .1f, "Mouse movement before focus does not rotate view");
        var allTargets = FindObjectsByType<QuietCornerTarget>(FindObjectsSortMode.None);
        int activations = allTargets.Sum(t => t.GetComponent<EZPZQuietCornerBridge>().activationCount);
        yield return Pointer(Vector2.zero, true);
        yield return Pointer(Vector2.zero);
        Check(Cursor.lockState == CursorLockMode.Locked, "First click enters captured mouse-look mode");
        Check(allTargets.Sum(t => t.GetComponent<EZPZQuietCornerBridge>().activationCount) == activations, "First click does not activate a scene control");
        yield return Pointer(new Vector2(120, 60));
        var rotated = rig.head.transform.rotation;
        Check(!mouse.leftButton.isPressed && !mouse.rightButton.isPressed && Quaternion.Angle(initial, rotated) > 10,
            "Mouse movement rotates view with NO mouse buttons held");
        Check(Mathf.Abs(Mathf.DeltaAngle(0, rig.head.transform.localEulerAngles.x)) > 5, "Unheld vertical movement changes pitch");
        yield return Frames(10);
        Check(Quaternion.Angle(rotated, rig.head.transform.rotation) < .1f, "Stationary mouse produces no drift or XR overwrite");
        yield return Pointer(new Vector2(-120, -60));
        Check(Quaternion.Angle(initial, rig.head.transform.rotation) < .1f, "Opposite unheld movement restores view");
        foreach (string action in new[] { "light", "sound", "pause", "stop", "reset", "teleport" })
        {
            // STOP and RESET deliberately release the cursor. Resume just as
            // a player does before aiming at the next control.
            if (Cursor.lockState != CursorLockMode.Locked)
            {
                yield return Pointer(Vector2.zero, true);
                yield return Pointer(Vector2.zero);
            }
            var target = allTargets.First(t => t.action == action);
            yield return AimAt(target);
            var bridge = target.GetComponent<EZPZQuietCornerBridge>();
            int before = bridge.activationCount;
            yield return Pointer(Vector2.zero, true);
            yield return Pointer(Vector2.zero);
            Check(bridge.activationCount == before + 1, "Centre-aim click invokes EZPZ " + action + " exactly once");
            if (action == "light") Check(!experience.warm, "Aim-and-click changes lighting");
            if (action == "sound") Check(experience.soundOn, "Aim-and-click turns on sound");
            if (action == "pause") Check(experience.guideRunning, "Aim-and-click starts guide");
            if (action == "stop") Check(!experience.soundOn && !experience.guideRunning, "STOP ends sound and guide");
            if (action == "reset") Check(experience.warm && !experience.soundOn && !experience.guideRunning, "RESET restores warm, silent, stopped state");
        }
        experience.Teleport(new Vector3(0,.05f,-4));
        rig.transform.rotation = Quaternion.identity;
        rig.head.transform.localRotation = Quaternion.identity;
        browser.SyncView();
        foreach (Key key in new[] { Key.UpArrow, Key.DownArrow, Key.LeftArrow, Key.RightArrow })
        {
            Vector3 from = rig.transform.position;
            Vector3 expected = key == Key.UpArrow ? rig.transform.forward : key == Key.DownArrow ? -rig.transform.forward
                : key == Key.LeftArrow ? -rig.transform.right : rig.transform.right;
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(key));
            yield return new WaitForSeconds(.3f);
            InputSystem.QueueStateEvent(keyboard, new KeyboardState());
            yield return Frames();
            Check(Vector3.Dot(rig.transform.position-from, expected)>.2f, key + " moves in the expected direction");
        }
        var standing = rig.transform.position;
        var standingView = rig.head.transform.rotation;
        InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.F));
        yield return Frames();
        InputSystem.QueueStateEvent(keyboard, new KeyboardState());
        yield return Frames();
        Check(experience.seated && experience.guideRunning, "F seats browser player and starts guided break");
        Check(Mathf.Abs(rig.head.transform.localPosition.y-1.35f)<.01f, "Seated camera height persists across frames");
        Check(Vector3.Dot(rig.head.transform.forward,(experience.orb.position-rig.head.transform.position).normalized)>.9f,
            "Seated camera keeps its new orientation toward the orb");
        var seatPosition = rig.transform.position;
        InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.UpArrow));
        yield return new WaitForSeconds(.3f);
        InputSystem.QueueStateEvent(keyboard, new KeyboardState());
        yield return Frames();
        Check(Vector3.Distance(seatPosition,rig.transform.position)<.01f,"Seated player stays still without gravity drift");
        yield return new WaitForEndOfFrame();
        var seatedImage = ScreenCapture.CaptureScreenshotAsTexture();
        File.WriteAllBytes(Path.Combine(output,"Sofa_Subtitles.png"),seatedImage.EncodeToPNG());
        Destroy(seatedImage);
        InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.F));
        yield return Frames();
        InputSystem.QueueStateEvent(keyboard, new KeyboardState());
        yield return Frames();
        Check(!experience.seated && !experience.guideRunning && Vector3.Distance(standing,rig.transform.position)<.05f,
            "F restores standing location and stops guide");
        Check(Quaternion.Angle(standingView,rig.head.transform.rotation)<.1f,"Standing restores the original view");
        // Compare actual displacement rather than just inspecting the configured speed.
        float[] distances = new float[4];
        Key[][] movementKeys = { new[]{Key.UpArrow},new[]{Key.UpArrow,Key.LeftShift},new[]{Key.UpArrow,Key.RightShift},new[]{Key.UpArrow} };
        for (int sample=0;sample<movementKeys.Length;sample++)
        {
            experience.Teleport(new Vector3(0,.05f,-5));
            rig.transform.rotation=Quaternion.identity;
            rig.head.transform.localRotation=Quaternion.identity;
            browser.SyncView();
            Vector3 start=rig.transform.position;
            InputSystem.QueueStateEvent(keyboard,new KeyboardState(movementKeys[sample]));
            yield return new WaitForSeconds(.4f);
            InputSystem.QueueStateEvent(keyboard,new KeyboardState());
            yield return Frames();
            distances[sample]=Vector3.Dot(rig.transform.position-start,Vector3.forward);
        }
        Check(distances[1]>distances[0]*1.5f && distances[1]<distances[0]*2.1f,"Left Shift increases actual movement speed by about 1.8x");
        Check(distances[2]>distances[0]*1.5f,"Right Shift also accelerates movement");
        Check(Mathf.Abs(distances[3]-distances[0])<.12f,"Releasing Shift restores walking speed");
        experience.Teleport(new Vector3(0,.05f,-7.2f));
        InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.DownArrow,Key.LeftShift));
        yield return new WaitForSeconds(.8f);
        InputSystem.QueueStateEvent(keyboard,new KeyboardState());
        yield return Frames();
        Check(rig.transform.position.z > -7.85f,"Sprinting respects the room boundary collider");

        var chairs=FindObjectsByType<QuietCornerSeat>(FindObjectsSortMode.None).OrderBy(s=>s.name).ToArray();
        Check(chairs.Length==6,"Three tables each have two interactive chairs");
        foreach(var chair in chairs)
        {
            experience.Teleport(chair.transform.position-chair.transform.forward*1.1f+Vector3.up*.05f);
            rig.transform.rotation=chair.transform.rotation;
            rig.head.transform.localRotation=Quaternion.identity;
            browser.SyncView();
            var beforeSeat=rig.transform.position;
            var target=chair.GetComponent<QuietCornerTarget>();
            yield return AimAt(target);
            int count=target.GetComponent<EZPZQuietCornerBridge>().activationCount;
            yield return Pointer(Vector2.zero,true);
            yield return Pointer(Vector2.zero);
            Check(experience.seated && experience.currentSeat==chair && !experience.guideRunning && target.GetComponent<EZPZQuietCornerBridge>().activationCount==count+1,
                chair.name+": centre click seats the player through EZPZ without starting a guide");
            Vector3 seatedAt=rig.transform.position;
            InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.UpArrow,Key.LeftShift));
            yield return new WaitForSeconds(.15f);
            InputSystem.QueueStateEvent(keyboard,new KeyboardState());
            yield return Frames();
            Check(Vector3.Distance(seatedAt,rig.transform.position)<.01f,chair.name+": Shift and movement cannot move a seated player");
            if(chair==chairs[0])
            {
                yield return new WaitForEndOfFrame();
                var picture=ScreenCapture.CaptureScreenshotAsTexture();
                File.WriteAllBytes(Path.Combine(output,"Cafe_Table_Seat.png"),picture.EncodeToPNG());
                Destroy(picture);
            }
            InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.F));
            yield return Frames();
            InputSystem.QueueStateEvent(keyboard,new KeyboardState());
            yield return Frames();
            Check(!experience.seated && Vector3.Distance(beforeSeat,rig.transform.position)<.06f,chair.name+": F restores a clear standing position");
        }
        var nearby=chairs[0];
        experience.Teleport(nearby.transform.position-nearby.transform.forward*1.1f+Vector3.up*.05f);
        InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.F));
        yield return Frames();
        InputSystem.QueueStateEvent(keyboard,new KeyboardState());
        yield return Frames();
        Check(experience.seated && experience.currentSeat==nearby,"F chooses a nearby table chair instead of the sofa");
        experience.StandUp();

        foreach(var chair in chairs)
        {
            experience.Teleport(chair.transform.position-chair.transform.forward*1.1f+Vector3.up*.05f);
            rig.transform.rotation=chair.transform.rotation;
            rig.head.transform.localRotation=Quaternion.identity;
            browser.SyncView();
            yield return Frames();
            var beforeChair=rig.transform.position;
            var beforeView=rig.head.transform.rotation;
            InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.E));
            yield return Frames(10);
            Check(experience.seated && experience.currentSeat==chair,chair.name+": E seats the player and holding it does not toggle repeatedly");
            Check(Mathf.Abs(Mathf.DeltaAngle(rig.transform.eulerAngles.y,chair.transform.eulerAngles.y))<.1f,
                chair.name+": E sitting does not also turn 30 degrees");
            InputSystem.QueueStateEvent(keyboard,new KeyboardState());
            yield return Frames();
            InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.E));
            yield return Frames();
            InputSystem.QueueStateEvent(keyboard,new KeyboardState());
            yield return Frames();
            Check(!experience.seated && Vector3.Distance(beforeChair,rig.transform.position)<.06f && Quaternion.Angle(beforeView,rig.head.transform.rotation)<.1f,
                chair.name+": second E restores standing position and view");
        }
        experience.Teleport(new Vector3(0,.05f,-5));
        rig.transform.rotation=Quaternion.identity;
        rig.head.transform.localRotation=Quaternion.identity;
        browser.SyncView();
        InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.E));
        yield return Frames();
        InputSystem.QueueStateEvent(keyboard,new KeyboardState());
        yield return Frames();
        Check(!experience.seated && Mathf.Abs(Mathf.DeltaAngle(rig.transform.eulerAngles.y,30))<.1f,
            "Away from chairs E still turns right without teleporting to the sofa");

        var vending=FindFirstObjectByType<QuietCornerVendingMachine>();
        experience.Teleport(new Vector3(3.15f,.05f,-5.85f));
        rig.transform.rotation=Quaternion.Euler(0,90,0);
        rig.head.transform.localRotation=Quaternion.identity;
        browser.SyncView();
        var water=allTargets.First(t=>t.action=="water");
        var tea=allTargets.First(t=>t.action=="tea");
        var tray=allTargets.First(t=>t.action=="takeDrink" && t.name.Contains("button"));
        foreach(var selection in new[]{water,tea})
        {
            yield return AimAt(selection);
            int before=selection.GetComponent<EZPZQuietCornerBridge>().activationCount;
            yield return Pointer(Vector2.zero,true);
            yield return Pointer(Vector2.zero);
            Check(vending.dispensing && selection.GetComponent<EZPZQuietCornerBridge>().activationCount==before+1,"Centre click starts vending "+selection.action);
            yield return new WaitForSeconds(.8f);
            string expected=selection.action=="tea" ? "Tea" : "Water";
            Check(vending.readyDrink==expected && vending.drink.gameObject.activeSelf,"Machine dispenses a visible "+expected);
            if(selection==water)
            {
                yield return new WaitForEndOfFrame();
                var machineImage=ScreenCapture.CaptureScreenshotAsTexture();
                File.WriteAllBytes(Path.Combine(output,"Cafe_Drinks_Machine.png"),machineImage.EncodeToPNG());
                Destroy(machineImage);
            }
            vending.SelectDrink(selection.action=="tea" ? "water" : "tea");
            Check(vending.readyDrink==expected && !vending.dispensing,"An uncollected drink cannot be overwritten");
            yield return AimAt(tray);
            yield return Pointer(Vector2.zero,true);
            yield return Pointer(Vector2.zero);
            Check(string.IsNullOrEmpty(vending.readyDrink) && !vending.drink.gameObject.activeSelf,"Clicking tray collects "+expected);
        }
        Check(vending.collectedDrinks==2,"Both drink options can be dispensed and collected");
        experience.Teleport(new Vector3(1,.05f,-5.85f));
        yield return AimAt(water);
        yield return Pointer(Vector2.zero,true);
        yield return Pointer(Vector2.zero);
        Check(!vending.dispensing && string.IsNullOrEmpty(vending.readyDrink),"Distant vending interaction asks player to move closer");

        InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.Escape));
        yield return Frames();
        InputSystem.QueueStateEvent(keyboard, new KeyboardState());
        yield return Frames();
        Check(Cursor.lockState == CursorLockMode.None && Cursor.visible, "Esc releases and shows mouse");
        var released = rig.head.transform.rotation;
        yield return Pointer(new Vector2(120, 60));
        Check(Quaternion.Angle(released, rig.head.transform.rotation) < .1f, "Released mouse no longer rotates view");
        yield return Pointer(Vector2.zero, true);
        yield return Pointer(Vector2.zero);
        yield return Pointer(new Vector2(80, 0));
        Check(Cursor.lockState == CursorLockMode.Locked && Quaternion.Angle(released, rig.head.transform.rotation) > 5,
            "Click resumes mouse-follow mode without holding a button");
        Check(errors == 0, "No runtime errors");
        File.WriteAllLines(Path.Combine(output, "MouseLook_Regression.txt"), checks);
        Application.logMessageReceived -= Log;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        InputSystem.RemoveDevice(mouse);
        InputSystem.RemoveDevice(keyboard);
        Debug.Log("MOUSE_LOOK_VERIFICATION failures=" + failures);
        Application.Quit(failures == 0 ? 0 : 1);
    }
}
