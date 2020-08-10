// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Quantum.QsCompiler.TargetGeneration

open System
open System.Collections.Generic
open System.IO
open Microsoft.CodeAnalysis
open Microsoft.Quantum.QsCompiler
open Microsoft.Quantum.QsCompiler.CsharpGeneration
open Microsoft.Quantum.QsCompiler.DataTypes
open Microsoft.Quantum.QsCompiler.Diagnostics
open Microsoft.Quantum.QsCompiler.ReservedKeywords
open Microsoft.Quantum.QsCompiler.SyntaxTree
open Microsoft.Quantum.QsCompiler.Transformations.BasicTransformations

open GenerateTarget

type Emitter() =

    let _AssemblyConstants = new Dictionary<_, _>()
    let mutable _Diagnostics = []

    let TargetQualifiedClassKey = "TargetClass"
    let ExtendsTargetKey = "TargetToExtend"

    interface IRewriteStep with

        member this.Name = "TargetGeneration"
        member this.Priority = -1 // doesn't matter because this rewrite step is the only one in the dll
        member this.AssemblyConstants = upcast _AssemblyConstants
        member this.GeneratedDiagnostics = upcast _Diagnostics
        
        member this.ImplementsPreconditionVerification = true
        member this.ImplementsPostconditionVerification = false
        member this.ImplementsTransformation = true

        member this.PreconditionVerification _ = 
            let step = this :> IRewriteStep
            step.AssemblyConstants.ContainsKey(TargetQualifiedClassKey)

        member this.PostconditionVerification _ = NotImplementedException() |> raise
        
        member this.Transformation (compilation, transformed) = 
            let step = this :> IRewriteStep
            let dir = step.AssemblyConstants.TryGetValue AssemblyConstants.OutputPath |> function
                | true, outputFolder when outputFolder <> null -> outputFolder
                | _ -> step.Name

            let fullName = step.AssemblyConstants.[TargetQualifiedClassKey]
            let targetNamespace = fullName.Substring(0, fullName.LastIndexOf('.'))
            let targetClassName = fullName.Substring(fullName.LastIndexOf('.') + 1)
            let baseClassName = match step.AssemblyConstants.TryGetValue ExtendsTargetKey with | (true, s) -> Some s | (false, _) -> None
            let outputFileName = Path.Combine(dir, targetClassName + ".cs")

            GenerateTarget compilation.Namespaces outputFileName targetClassName targetNamespace baseClassName

            transformed <- compilation
            true
