using dnlib.DotNet;
using dnlib.DotNet.Emit;
using System;
using System.Linq;

class Program
{
    static void Main(string[] args)
    {
        if (args.Length < 2)
        {
            Console.WriteLine("Usage: patcher <input> <output>");
            return;
        }

        string input = args[0];
        string output = args[1];

        ModuleDefMD module = ModuleDefMD.Load(input);

        Console.WriteLine("[*] Injecting CheatInit...");

        InjectCheatInit(module);

        Console.WriteLine("[*] Saving patched assembly...");
        module.Write(output);
        Console.WriteLine("[+] Done!");
    }

    static void InjectCheatInit(ModuleDefMD module)
    {
        var programType = new TypeDefUser("InjectedCheats", "CheatInit",
            module.CorLibTypes.Object.TypeDefOrRef);
        module.Types.Add(programType);

        var method = new MethodDefUser("InitCheats",
            MethodSig.CreateStatic(module.CorLibTypes.Void),
            MethodImplAttributes.IL | MethodImplAttributes.Managed,
            MethodAttributes.Public | MethodAttributes.Static);

        programType.Methods.Add(method);

        var il = method.Body.Instructions;

        il.Add(OpCodes.Nop.ToInstruction());

        il.Add(OpCodes.Ldstr.ToInstruction("Injected cheats active"));
        il.Add(OpCodes.Call.ToInstruction(
            module.Import(typeof(Console).GetMethod("WriteLine", new Type[] { typeof(string) }))));

        il.Add(OpCodes.Nop.ToInstruction());

        il.Add(OpCodes.Call.ToInstruction(
            module.Import(typeof(Program).GetMethod("RunCheatLoop",
                System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic))));

        il.Add(OpCodes.Ret.ToInstruction());
    }

    static void RunCheatLoop()
    {
        new System.Threading.Thread(() =>
        {
            while (true)
            {
                try
                {
                    foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
                    {
                        foreach (var type in asm.GetTypes())
                        {
                            foreach (var field in type.GetFields(
                                System.Reflection.BindingFlags.Public |
                                System.Reflection.BindingFlags.NonPublic |
                                System.Reflection.BindingFlags.Instance |
                                System.Reflection.BindingFlags.Static))
                            {
                                string n = field.Name.ToLower();

                                if (n.Contains("hp") || n.Contains("health"))
                                    TrySet(field, 999999);

                                if (n.Contains("ammo") || n.Contains("bullet") || n.Contains("clip"))
                                    TrySet(field, 9999);

                                if (n.Contains("grenade"))
                                    TrySet(field, 999);
                            }
                        }
                    }
                }
                catch { }
                System.Threading.Thread.Sleep(50);
            }
        }).Start();
    }

    static void TrySet(System.Reflection.FieldInfo field, object value)
    {
        try
        {
            if (!field.IsStatic) return;
            field.SetValue(null, Convert.ChangeType(value, field.FieldType));
        }
        catch { }
    }
}
