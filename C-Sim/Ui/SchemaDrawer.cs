﻿// CSim - (c) 2014-17 Baltasar MIT License <jbgarcia@uvigo.es>

namespace CSim.Ui {
    using System.Numerics;
	using System.Drawing;
    using System.Collections.Generic;

	using Core;
	using Core.Variables;
	using Core.Types;
	using Ui.Drawer;

	/// <summary>
	/// Draws the schema of the variables present in memory
	/// </summary>
    public class SchemaDrawer {
		/// <summary>The horizontal gap between boxes of variables.</summary>
		public const int HGap = 15;
		/// <summary>The vertical gap between boxes of variables.</summary>
		public const int VGap = 50;
		/// <summary>Maximum number of boxes of variables per row.</summary>
		public const int MaxPerRow = 5;
		/// <summary>The <see cref="System.Drawing.Color"/> of the background.</summary>
		public readonly Color BackgroundColor = Color.GhostWhite;

		/// <summary>
		/// Initializes a new instance of the <see cref="CSim.Ui.SchemaDrawer"/> class.
		/// </summary>
		/// <param name="m">The machine from which to draw the schema of its memory</param>
		public SchemaDrawer(Machine m)
        {
            this.Machine = m;

			var normalFont = new Font( FontFamily.GenericMonospace, 12 );
			var smallFont = new Font( FontFamily.GenericMonospace, 10 );
			var pen = new Pen( Brushes.Navy );

			this.GraphInfo = new GraphInfo( null, pen, BackgroundColor, smallFont, normalFont, HGap, VGap );
        }

		/// <summary>
		/// Inits the graphics.
		/// This is interesting for <see cref="CalculateSizes()"/> to work, since it needs
		/// font info and so on.
		/// </summary>
		/// <param name="bitmap">The bitmap in which to draw.</param>
		public void InitGraphics(Bitmap bitmap)
        {
            this.bmBoard = bitmap;
            this.board = Graphics.FromImage( this.bmBoard );
			this.GraphInfo.Graphics = this.board;
        }
        
        private IEnumerable<IEnumerable<Variable>> ClassifyMachineVariables()
        {
            return ClassifyVariables( this.Machine.TDS.Variables );
        }

		private IEnumerable<IEnumerable<Variable>>
                                        ClassifyVariables(IList<Variable> vbles)
		{
			var primitives = new List<Variable>();
			var pointersByLevel = new Dictionary<int, List<Variable>>();
			var arrays = new List<Variable>();
			var lists = new List<List<Variable>> {
				primitives,
				arrays
			};
            
            // Create first level of pointers
            pointersByLevel[ 1 ] = new List<Variable>();

			// Classify by type
			foreach (Variable v in vbles) {
                if ( v.IsTemp() ) {
                    continue;
                }
            
                if ( v is RefVariable ) {
                    pointersByLevel[ 1 ].Add( v );
                }
                else
				if ( v is PtrVariable ptrVble ) {
					int level = ( (Ptr) ptrVble.Type ).IndirectionLevel;
					List<Variable> ptrs;

					if ( !pointersByLevel.TryGetValue( level, out ptrs ) ) {
						pointersByLevel[ level ] = new List<Variable>();
					}

					pointersByLevel[ level ].Add( ptrVble );
				}
				else if ( v is ArrayVariable ) {
					arrays.Add( v );
				}
				else {
					primitives.Add( v );
				}
			}

			// Add pointers lists
			var keys = new List<int>( pointersByLevel.Keys );
			keys.Sort();

			foreach (int level in keys) {
				lists.Add( pointersByLevel[ level ] );
			}

			return lists.ToArray();
		}

		/// <summary>
		/// Calculates the sizes of all boxes.
		/// </summary>
		public void CalculateSizes()
		{
			IEnumerable<IEnumerable<Variable>> lists = ClassifyMachineVariables();

			this.boxes = new GrphBoxesPerAddress();
			this.rows = new GrphRows( HGap, VGap, MaxPerRow );

            // Insert all "main" variables
			foreach (IEnumerable<Variable> list in lists) {
				foreach (Variable v in list) {
	                var box = GrphBoxedVariable.Create( v, this.GraphInfo );
	                this.rows.AddBox( box );
                    this.boxes.Add( box );
                    
                }
				this.rows.ForceNewRow();
			}
            
            this.Size = this.rows.CalculateAreaSize();
            
            // Insert all array elements (not drawn, only for relationships)
            var arrayElements = new List<GrphBoxedVariable>();
            
            foreach(GrphBoxedVariable box in this.boxes.AllBoxes) {
                if ( box is GrphBoxedArray arrayBox
                  && arrayBox.ArrayVariable.ElementsType is Indirection )
                {
                    box.GetInvolvedBoxes().ForEach( ( b ) =>
                    {
                        b.Y = box.Y;
                        b.X += box.X;
                        arrayElements.Add( b );
                    });
                }
            }
            
            this.boxes.AddRange( arrayElements );
			return;
		}

		private void Cls()
		{
			this.board.Clear( this.GraphInfo.BackGroundColor );
		}

		/// <summary>
		/// Draw the specified schema on the given bitmap.
		/// </summary>
		/// <param name="surface">The bitmap to draw on.</param>
        public void Draw(Bitmap surface)
		{        
			this.InitGraphics( surface );
			this.Cls();
  
            // Draw relationships
            foreach(GrphBoxedVariable box in this.boxes.AllBoxes) {
                if ( box.Variable.IsIndirection() ) {
                    this.DrawRelationship( box );
                }
            }
            
			// Draw the boxes themselves
			foreach(GrphBoxedVariable box in this.boxes.AllBoxes) {
                box.Draw();
			}

            return;
        }

		private void DrawRelationship(GrphBoxedVariable box)
		{
			if ( box.Variable.IsIndirection()
              && !( box is GrphBoxedArray ) )
            {
                var indirectVble = (IndirectVariable) box.Variable;
				BigInteger address = indirectVble.PointedAddress;
				GrphBoxedVariable pointedBox = null;
                IList<GrphBoxedVariable> l = this.boxes.GetBoxesForAddress( address );
                
				if ( l.Count > 0 ) {
                    pointedBox = l[0];
				    float delta = 0;

					if ( !( pointedBox is Drawer.GrphBoxedArray ) ) {
						delta = ( pointedBox.Width / 2 ) - pointedBox.BoxX;
					}

                    if ( pointedBox != box ) {
						this.DrawConnection(
							box.X + box.BoxX + ( box.BoxWidth / 2 ), box.Y - 5,
							pointedBox.X + pointedBox.BoxX + delta,
                            pointedBox.Y + pointedBox.Height + 5
						);
                    }
				} else {
                    if ( !( box is GrphBoxedArrayElement ) ) {
	                    if ( address == 0 )
	                    {
	                        this.DrawNullPointer( box );
	                    } else {
							this.GraphInfo.Pen.Color = Color.OrangeRed;
							var start = new Point( (int) ( box.X + box.BoxX + ( box.BoxWidth / 2 ) ), (int) ( box.Y - 5 ) );
							var end = new Point( (int) ( box.X + box.BoxX + ( box.BoxWidth / 2 ) ), (int) ( box.Y - 25 ) );
		
							this.board.DrawLine( this.GraphInfo.Pen, start, end );
							this.board.DrawLine( this.GraphInfo.Pen, end.X - 10, end.Y, end.X + 10, end.Y );
							this.GraphInfo.Pen.Color = Color.Black;
	                    }
                    }
				}
			}

			return;
		}

		private void DrawConnection(float x1, float y1, float x2, float y2)
		{
			this.GraphInfo.Pen.Color = Color.DarkGray;

			// Line
			this.board.DrawLine( this.GraphInfo.Pen, x1, y1, x2, y2 + 20 );

			// Arrow
			var vertices = new [] { 
				new Point( (int) ( x2 - 10 ), (int) ( y2 + 20 ) ),
				new Point( (int) ( x2 + 10 ), (int) ( y2 + 20 ) ),
				new Point( (int) ( x2 + 5 ), (int) y2 )
			};
			this.board.FillPolygon( this.GraphInfo.Pen.Brush, vertices );

			this.GraphInfo.Pen.Color = Color.Black;
		}

		private void DrawNullPointer(GrphBoxedVariable box)
		{
			var start = new Point( (int) ( box.X + box.BoxX + ( box.BoxWidth / 2 ) ), (int) ( box.Y -5 ) );
			var end1 = new Point( start.X, start.Y -20 );
			var end2 = new Point( end1.X + 20, end1.Y );
			var end3 = new Point( end2.X, end2.Y + 10 );

			// The semi-rectangle
			this.GraphInfo.Pen.Color = Color.DarkGray;
			this.board.DrawLine( this.GraphInfo.Pen, start, end1 );
			this.board.DrawLine( this.GraphInfo.Pen, end1, end2 );
			this.board.DrawLine( this.GraphInfo.Pen, end2, end3 );

			// The "ground" mark
			this.board.DrawLine(
				this.GraphInfo.Pen, end3.X - 10, end3.Y, end3.X + 10, end3.Y
			);

			this.board.DrawLine(
				this.GraphInfo.Pen, end3.X - 7, end3.Y + 2, end3.X + 7, end3.Y + 2
			);

			this.board.DrawLine(
				this.GraphInfo.Pen, end3.X - 3, end3.Y + 4, end3.X + 3, end3.Y + 4
			);

			this.GraphInfo.Pen.Color = Color.Black;
		}

		/// <summary>
		/// Updates the fonts, so the schema looks bigger or smaller.
		/// </summary>
		/// <param name="step">How many points to increase or decrease the font.</param>
		public void UpdateFont(int step)
		{
			var normalFont = new Font(
				this.NormalFont.Font.FontFamily,
				this.NormalFont.Font.Size + step );
			var smallFont = new Font(
				this.SmallFont.Font.FontFamily,
				this.SmallFont.Font.Size + step );

			this.NormalFont.Font = normalFont;
			this.SmallFont.Font = smallFont;
		}

		/// <summary>
		/// Gets the normal font.
		/// </summary>
		/// <value>The normal font, as a Font instance.</value>
        public FontInfo NormalFont {
			get {
				return this.GraphInfo.NormalFont;
			}
        }

		/// <summary>
		/// Gets the small font.
		/// </summary>
		/// <value>The small font, as a Font instance.</value>
		public FontInfo SmallFont {
			get {
				return this.GraphInfo.SmallFont;
			}
        }

		/// <summary>
		/// Gets the pen used to draw.
		/// </summary>
		/// <value>The pen, as a Pen instance.</value>
		public Pen Pen {
			get {
				return this.GraphInfo.Pen;
			}
		}

		/// <summary>
		/// Gets the graph info objects, which includes all graphics sets.
		/// </summary>
		/// <value>The graph info, as a GraphInfo instance.</value>
		public GraphInfo GraphInfo {
			get; private set;
		}

		/// <summary>
		/// Gets the size of the schema, once sizes are calculated.
		/// </summary>
		/// <value>The size, as a <see cref="Size"/> object.</value>
		public Size Size {
			get; private set;
		}

		/// <summary>
		/// Gets or sets the machine the schema is going to be drawn for.
		/// </summary>
		/// <value>The machine, as a Machine instance.</value>
		public Machine Machine {
			get; set;
		}

        private Bitmap bmBoard;
        private Graphics board;
		private GrphRows rows;
        private GrphBoxesPerAddress boxes;
    }
}

