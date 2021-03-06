﻿// CSim - (c) 2014-17 Baltasar MIT License <jbgarcia@uvigo.es>

namespace CSim.Core.Opcodes {
	/// <summary>
	/// Create opcode. Allows to create different kinds of variables.
	/// </summary>
	public class CreateOpcode: Opcode {
		/// <summary>The opcode's representing value.</summary>
		public static byte OpcodeValue = 0xE3;

		/// <summary>
		/// Initializes a new instance of the <see cref="T:CSim.Core.Opcodes.CreateOpcode"/> class.
		/// </summary>
		/// <param name="m">The <see cref="Machine"/> this opcode will be executed in.</param>
		/// <param name="n">The name of the <see cref="Variable"/>.</param>
		/// <param name="t">The <see cref="AType"/> of the new <see cref="Variable"/>.
		/// It can be complex (i.e., a <see cref="CSim.Core.Types.Ref"/>
		/// or a <see cref="CSim.Core.Types.Ptr"/></param>
		public CreateOpcode(Machine m, Id n, AType t)
            : base( m )
		{
			this.Name = n;
			this.Type = t;
		}

		/// <summary>
		/// Execute the opcode.
		/// </summary>
		public override void Execute()
		{
            var result = this.Type.CreateVariable( this.Name.Text );
            this.Machine.TDS.Add( result );
            
            this.Machine.ExecutionStack.Push( result );
		}

		/// <summary>
		/// Gets or sets the name of the new <see cref="Variable"/>
		/// </summary>
		/// <value>The name.</value>
		public Id Name {
            get; set;
        }

		/// <summary>
		/// Gets or sets the type of the new <see cref="Variable"/>
		/// </summary>
		/// <value>The type, as a <see cref="AType"/>.</value>
		public AType Type {
			get; set;
		}
        
        /// <summary>
        /// Returns a <see cref="T:System.String"/> that represents the current <see cref="T:CSim.Core.Opcodes.CreateOpcode"/>.
        /// </summary>
        /// <returns>A <see cref="T:System.String"/> that represents the current <see cref="T:CSim.Core.Opcodes.CreateOpcode"/>.</returns>
        public override string ToString()
        {
            return string.Format( "[CreateOpcode(0x{0,2:X}): lvalue(PUSH): '{1} {2}']",
                                    OpcodeValue,
                                    this.Type,
                                    this.Name );
        }
	}
}
