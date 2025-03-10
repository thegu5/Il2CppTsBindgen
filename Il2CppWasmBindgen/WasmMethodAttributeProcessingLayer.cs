using Cpp2IL.Core.Api;
using Cpp2IL.Core.Model.Contexts;
using Cpp2IL.Core.Utils;
using LibCpp2IL;
using LibCpp2IL.Wasm;

namespace Il2CppWasmBindgen;

public class WasmMethodAttributeProcessingLayer : Cpp2IlProcessingLayer
{
    public override void Process(ApplicationAnalysisContext appContext, Action<int, int>? progressCallback = null)
    {
        var methodIndexAttributes = AttributeInjectionUtils.InjectTwoParameterAttribute(appContext, "Cpp2ILInjected",
            "WasmMethod", AttributeTargets.Method, false, appContext.SystemTypes.SystemInt32Type, "Index", appContext.SystemTypes.SystemInt32Type, "Pointer");
        
        foreach (var assembly in appContext.Assemblies)
        {
            var methodIndexAttributeInfo = methodIndexAttributes[assembly];
            
            foreach (var method in assembly.Types.SelectMany(t => t.Methods))
            {
                method.AnalyzeCustomAttributeData();
                if (method.Definition is null || method.CustomAttributes == null || method.UnderlyingPointer == 0) continue;
                
                var wasmdef = WasmUtils.TryGetWasmDefinition(method.Definition);
                if (wasmdef is null) continue;
                
                AttributeInjectionUtils.AddTwoParameterAttribute(method, methodIndexAttributeInfo, wasmdef.IsImport
                    ? ((WasmFile)LibCpp2IlMain.Binary!).FunctionTable.IndexOf(wasmdef)
                    : wasmdef.FunctionTableIndex, Convert.ToInt32(method.Definition.MethodPointer));
            }
        }
    }

    public override string Name => "Wasm Method Attribute Injector";
    public override string Id => "WasmMethodAttributeProcessingLayer";
}