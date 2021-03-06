﻿// CSim - (c) 2014-17 Baltasar MIT License <jbgarcia@uvigo.es>

namespace CSim.Core {
	using Types;

    /// <summary>
    /// Represents all available types, i.e.: int, float, double...
    /// </summary>
    public abstract class AType: RValue {
		/// <summary>
		/// Initializes a new instance of the <see cref="AType"/> class.
		/// </summary>
		/// <param name="n">The name of the type.</param>
		/// <param name="s">Its size, in bytes</param>
        /// <param name="m">The machine this RValue will be evaluated for.</param>
        protected AType(Machine m, string n, int s)
            :base( m )
        {
            this.Name = n;
            this.Size = s;
        }

        /// <summary>
        /// Gets the type of this <see cref="AType"/>.
        /// </summary>
        /// <returns>Itself.</returns>
        public override AType Type {
            get {
                return this;
            }
        }

        /// <summary>
        /// Gets the "value" of this Type.
        /// </summary>
        /// <value>The size of the type.</value>
        public override object Value {
            get {
                return this.Size;
            }
        }

        /// <summary>
        /// Gets the name.
        /// </summary>
        /// <value>
        /// The name of the type
        /// </value>
        public string Name {
			get; private set;
        }

        /// <summary>
        /// Gets the size
        /// </summary>
        /// <value>
        /// The size in bytes
        /// </value>
        public int Size {
			get; private set;
        }

		/// <summary>
		/// Creates a corresponding variable.
		/// </summary>
		/// <returns>A <see cref="Variable"/> with this <see cref="AType"/>.</returns>
		/// <param name="strId">The identifier for the variable.</param>
		public abstract Variable CreateVariable(string strId);

		/// <summary>
		/// Creates a literal for this type, given a byte sequence.
		/// </summary>
		/// <returns>The literal, as an appropriate object of a class inheriting from Literal.</returns>
		/// <param name="raw">The sequence of bytes containing the value in memory.</param>
        public abstract Literal CreateLiteral(byte[] raw);

		/// <summary>
		/// Creates a literal for this type, given a value.
		/// </summary>
		/// <returns>The literal, as an appropriate object of a class inheriting from Literal.</returns>
		/// <param name="v">The given value.</param>
		public abstract Literal CreateLiteral(object v);
        
        /// <summary>
        /// Solves the type to a variable with the <see cref="Literals.TypeLiteral"/> as value.
        /// </summary>
        /// <returns>A suitable <see cref="Variable"/>.</returns>
        public override Variable SolveToVariable()
        {
            var toret = Variable.CreateTempVariable( this );
            
            toret.LiteralValue = new Literals.TypeLiteral( this );
            return toret;
        }

		/// <summary>
		/// Determines whether this type is any,
		/// or a "de facto" <see cref="Any"/>.
		/// </summary>
		/// <returns><c>true</c> if this instance is any or similar; otherwise, <c>false</c>.</returns>
		public bool IsAny()
        {
			bool toret = ( this is Any );

			// "any" type (void*) or pointer to any.
			if ( !toret
              && this is Ptr ptr )
            {
				toret = ( ptr.AssociatedType is Any );
			}

			return toret;
		}

		/// <summary>
		/// Determines whether this instance is compatible with another, given one.
		/// </summary>
		/// <returns><c>true</c> if this instance is compatible with the specified other; otherwise, <c>false</c>.</returns>
		/// <param name="other">The other <see cref="AType"/>.</param>
		public bool IsCompatibleWith(AType other)
		{
            AType t1 = this;
            AType t2 = other;
			bool toret = ( t1.IsAny() || t2.IsAny() );
            
            // Maybe one of them is a reference
            if ( !toret ) {
	            if ( t1 is Ref refType1 ) {
	                t1 = refType1.AssociatedType;
	            }
	            
	            if ( t2 is Ref refType2 ) {
	                t2 = refType2.AssociatedType;
	            }
            }
            
			// Maybe they are just the same
			if ( !toret ) {
				toret = ( t1 == t2 );
			}

			// They are both pointers
			if ( !toret ) {
				toret = ( t1 is Ptr && t2 is Ptr );
			}

			// One of them is a pointer and the other a numeric value
			if ( !toret ) {
				toret = ( ( t1 is Ptr && t2 is Primitive )
				  	   || ( t1 is Primitive && t2 is Ptr ) );
			}

			// Compatibility between arithmetic types
			if ( !toret ) {
				toret = ( this is Primitive || t2 is Primitive );
			}
 
			return toret;
		}
        
        /// <summary>
        /// Returns a <see cref="System.String"/> that represents the current <see cref="AType"/>.
        /// </summary>
        /// <returns>A <see cref="System.String"/> that represents the current <see cref="AType"/>.</returns>
        public override string ToString()
        {
            return this.Name;
        }
        
        /// <summary>
        /// Is the identifier passed of a basic type?
        /// </summary>
        /// <returns><c>true</c>, if identifier pertains to a basic type, <c>false</c> otherwise.</returns>
        /// <param name="id">The identifier, as a string.</param>
        public static bool IsIdOfBasicType(string id)
        {
            return ( Primitive.IsPrimitive( id )
                  || id == TypeType.TypeName
                  || id == Any.TypeName );
        }        
    }
}
