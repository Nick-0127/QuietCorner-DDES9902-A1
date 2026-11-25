using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

// Rebuilds only the new furniture group; the existing room remains authored in its scene.
public static class QuietCornerCafeSetup
{
    static Material oak, sage, linen, ink, metal, brass, textMaterial;

    public static void Ensure(Material worldText)
    {
        var previous = GameObject.Find("06 Tables and refreshments");
        if (previous != null) Object.DestroyImmediate(previous);
        var root = new GameObject("06 Tables and refreshments").transform;
        textMaterial = worldText;
        oak = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Oak.mat");
        sage = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Sage.mat");
        linen = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Linen.mat");
        ink = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Ink.mat");
        metal = Material("Cafe Sage Metal", new Color(.19f,.31f,.29f));
        brass = Material("Cafe Brass", new Color(.73f,.58f,.34f));
        Table(root, 1, new Vector3(-3.35f,0,-5.15f));
        Table(root, 2, new Vector3(3.2f,0,-3.8f));
        Table(root, 3, new Vector3(-3.45f,0,-.65f));
        Vending(root);
    }

    static void Table(Transform root, int number, Vector3 position)
    {
        var table = new GameObject("Cafe table " + number).transform;
        table.SetParent(root, false);
        table.position = position;
        Shape(table, PrimitiveType.Cylinder,"Round oak tabletop",new Vector3(0,.79f,0),new Vector3(1.45f,.055f,1.45f),oak);
        Shape(table, PrimitiveType.Cylinder,"Table pedestal",new Vector3(0,.39f,0),new Vector3(.12f,.38f,.12f),ink);
        Shape(table, PrimitiveType.Cylinder,"Table base",new Vector3(0,.045f,0),new Vector3(.65f,.045f,.65f),ink);
        Shape(table, PrimitiveType.Cylinder,"Small ceramic vase",new Vector3(.15f,.94f,.1f),new Vector3(.15f,.1f,.15f),linen,false);
        Shape(table, PrimitiveType.Sphere,"Vase greenery",new Vector3(.15f,1.09f,.1f),new Vector3(.29f,.25f,.29f),sage,false);
        Chair(table, number, "A", new Vector3(0,0,-1.05f),0);
        Chair(table, number, "B", new Vector3(0,0,1.05f),180);
    }

    static void Chair(Transform table, int number, string side, Vector3 position, float yaw)
    {
        var chair = new GameObject("Table " + number + " chair " + side).transform;
        chair.SetParent(table, false);
        chair.localPosition = position;
        chair.localRotation = Quaternion.Euler(0,yaw,0);
        var seat = chair.gameObject.AddComponent<QuietCornerSeat>();
        seat.seatName = "Table " + number;
        var target = chair.gameObject.AddComponent<QuietCornerTarget>();
        target.action = "seat";
        chair.gameObject.AddComponent<EZPZQuietCornerBridge>();
        Shape(chair,PrimitiveType.Cylinder,"Sage seat cushion",new Vector3(0,.49f,0),new Vector3(.62f,.065f,.62f),sage);
        Shape(chair,PrimitiveType.Cube,"Oak chair back",new Vector3(0,.81f,-.29f),new Vector3(.65f,.57f,.09f),oak);
        foreach (float x in new[]{-.21f,.21f})
            foreach (float z in new[]{-.2f,.2f})
                Shape(chair,PrimitiveType.Cube,"Chair leg",new Vector3(x,.23f,z),new Vector3(.065f,.46f,.065f),oak);
        Shape(chair,PrimitiveType.Cube,"Seat label plaque",new Vector3(0,.84f,-.344f),new Vector3(.5f,.21f,.025f),ink,false);
        Label(chair,"E - SIT",new Vector3(0,.84f,-.36f),.018f);
    }

    static void Vending(Transform root)
    {
        var machineRoot = new GameObject("Quiet Corner drinks machine").transform;
        machineRoot.SetParent(root,false);
        machineRoot.SetPositionAndRotation(new Vector3(5.16f,0,-5.85f),Quaternion.Euler(0,90,0));
        Shape(machineRoot,PrimitiveType.Cube,"Machine cabinet",new Vector3(0,1.18f,0),new Vector3(1.4f,2.36f,.74f),metal);
        Shape(machineRoot,PrimitiveType.Cube,"Top trim",new Vector3(0,2.38f,0),new Vector3(1.45f,.07f,.78f),oak);
        Shape(machineRoot,PrimitiveType.Cube,"Display panel",new Vector3(0,1.76f,-.378f),new Vector3(1.2f,.84f,.035f),ink,false);
        Label(machineRoot,"DRINKS",new Vector3(0,2.04f,-.402f),.045f);
        Label(machineRoot,"A LITTLE REFRESHMENT",new Vector3(0,1.84f,-.402f),.014f);
        var machine = machineRoot.gameObject.AddComponent<QuietCornerVendingMachine>();
        machine.status = Label(machineRoot,"CHOOSE A DRINK",new Vector3(0,1.6f,-.405f),.017f);
        Button(machineRoot,"water","WATER",new Vector3(-.32f,1.17f,-.405f),new Vector3(.54f,.29f,.07f));
        Button(machineRoot,"tea","TEA",new Vector3(.32f,1.17f,-.405f),new Vector3(.54f,.29f,.07f));
        Shape(machineRoot,PrimitiveType.Cube,"Drink slot",new Vector3(0,.64f,-.39f),new Vector3(1.12f,.54f,.04f),ink,false);
        Shape(machineRoot,PrimitiveType.Cube,"Drink tray ledge",new Vector3(0,.38f,-.48f),new Vector3(1.15f,.065f,.31f),brass);
        Button(machineRoot,"takeDrink","TAKE DRINK",new Vector3(0,.31f,-.43f),new Vector3(.95f,.18f,.08f));
        var trayTarget = Shape(machineRoot,PrimitiveType.Cube,"Clickable collection tray",new Vector3(0,.65f,-.53f),new Vector3(1.1f,.47f,.21f),ink);
        trayTarget.GetComponent<Renderer>().enabled=false;
        trayTarget.AddComponent<QuietCornerTarget>().action="takeDrink";
        trayTarget.AddComponent<EZPZQuietCornerBridge>();
        var cupMaterial = Material("Cafe Drink Cup",new Color(.35f,.7f,.85f));
        machine.drink = Shape(machineRoot,PrimitiveType.Cylinder,"Dispensed drink",new Vector3(0,.55f,-.52f),new Vector3(.2f,.14f,.2f),cupMaterial,false).transform;
        machine.drinkSurface=machine.drink.GetComponent<Renderer>();
        Shape(machine.drink,PrimitiveType.Cylinder,"Cream cup lid",new Vector3(0,1.02f,0),new Vector3(1.09f,.08f,1.09f),linen,false);
        Label(machineRoot,"CHOOSE  >  TAKE  >  RELAX",new Vector3(0,.12f,-.403f),.014f);
    }

    static void Button(Transform parent,string action,string caption,Vector3 position,Vector3 scale)
    {
        var button=Shape(parent,PrimitiveType.Cube,caption+" button",position,scale,brass);
        button.AddComponent<QuietCornerTarget>().action=action;
        button.AddComponent<EZPZQuietCornerBridge>();
        Label(parent,caption,position+Vector3.back*(scale.z*.5f+.009f),.022f);
    }

    static Material Material(string name,Color color)
    {
        string path="Assets/Materials/"+name+".mat";
        var value=AssetDatabase.LoadAssetAtPath<Material>(path);
        if(value==null){value=new Material(Shader.Find("Standard"));AssetDatabase.CreateAsset(value,path);}
        value.color=color;
        value.SetFloat("_Glossiness",.18f);
        EditorUtility.SetDirty(value);
        return value;
    }
    static GameObject Shape(Transform parent,PrimitiveType type,string name,Vector3 position,Vector3 scale,Material material,bool collider=true)
    {
        var value=GameObject.CreatePrimitive(type);
        value.name=name;
        value.transform.SetParent(parent,false);
        value.transform.localPosition=position;
        value.transform.localScale=scale;
        value.GetComponent<Renderer>().sharedMaterial=material;
        if(!collider)Object.DestroyImmediate(value.GetComponent<Collider>());
        return value;
    }
    static TextMesh Label(Transform parent,string text,Vector3 position,float size)
    {
        var value=new GameObject(text).AddComponent<TextMesh>();
        value.transform.SetParent(parent,false);
        value.transform.localPosition=position;
        value.text=text;
        value.font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        value.fontSize=64;
        value.characterSize=size;
        value.anchor=TextAnchor.MiddleCenter;
        value.alignment=TextAlignment.Center;
        value.color=linen.color;
        value.GetComponent<MeshRenderer>().sharedMaterial=textMaterial;
        return value;
    }
}
