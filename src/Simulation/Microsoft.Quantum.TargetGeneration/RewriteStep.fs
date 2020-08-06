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


type Emitter() =

    let _AssemblyConstants = new Dictionary<_, _>()
    let mutable _Diagnostics = []

    interface IRewriteStep with

        member this.Name = "TargetGeneration"
        member this.Priority = -1 // doesn't matter because this rewrite step is the only one in the dll
        member this.AssemblyConstants = upcast _AssemblyConstants
        member this.GeneratedDiagnostics = upcast _Diagnostics
        
        member this.ImplementsPreconditionVerification = false
        member this.ImplementsPostconditionVerification = false
        member this.ImplementsTransformation = true

        member this.PreconditionVerification _ = NotImplementedException() |> raise

        member this.PostconditionVerification _ = NotImplementedException() |> raise
        
        member this.Transformation (compilation, transformed) = 
            let step = this :> IRewriteStep
            let dir = step.AssemblyConstants.TryGetValue AssemblyConstants.OutputPath |> function
                | true, outputFolder when outputFolder <> null -> outputFolder
                | _ -> step.Name

            let context = TargetGenerationContext.Create(compilation.Namespaces, step.AssemblyConstants)
            context.GenerateTarget(Path.Combine(dir, "Target.cs"))

            transformed <- compilation
            true
