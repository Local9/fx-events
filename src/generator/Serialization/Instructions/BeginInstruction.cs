using System;
using System.Collections.Generic;
using System.IO;
using Lusive.Events.Generator.Schemas;
using Lusive.Events.Generator.Serialization.Generation;
using Lusive.Events.Generator.Syntax;

namespace Lusive.Events.Generator.Serialization.Instructions
{
    public class BeginInstruction : BaseInstruction
    {
        public override InstructionType InstructionType => InstructionType.None;
        public IEnumerable<Tuple<SchemaMember, BaseInstruction[]>> Members { get; set; }

        public BeginInstruction(IEnumerable<Tuple<SchemaMember, BaseInstruction[]>> members)
        {
            Members = members;
        }

        public override void Build(GenerationContext context, InstructionHost host, SchemaMember member,
            CodeWriter code)
        {
        }

        protected override void Serialize(BinaryWriter writer)
        {
        }
    }
}