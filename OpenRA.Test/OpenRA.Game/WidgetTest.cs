#region Copyright & License Information
/*
 * Copyright (c) The OpenRA Developers and Contributors
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using NUnit.Framework;
using OpenRA.Support;
using OpenRA.Widgets;

namespace OpenRA.Test
{
	[TestFixture]
	sealed class WidgetTest
	{
		sealed class TestWidget : Widget { }

		[TestCase(TestName = "RecalculateLayout updates expression bounds and preserves imperative bounds")]
		public void TestRecalculateLayout()
		{
			var root = new TestWidget
			{
				Bounds = new WidgetBounds(0, 0, 1280, 720)
			};

			var centeredPanel = new TestWidget
			{
				X = new IntegerExpression("(PARENT_WIDTH - WIDTH) / 2"),
				Y = new IntegerExpression("(PARENT_HEIGHT - HEIGHT) / 2"),
				Width = new IntegerExpression("600"),
				Height = new IntegerExpression("400")
			};

			var imperativeChild = new TestWidget
			{
				Bounds = new WidgetBounds(25, 35, 120, 48)
			};

			var scrollChild = new TestWidget
			{
				X = new IntegerExpression("10"),
				Y = new IntegerExpression("20"),
				Width = new IntegerExpression("PARENT_WIDTH - 20"),
				Height = new IntegerExpression("30"),
				Bounds = new WidgetBounds(10, 150, 580, 30)
			};

			root.AddChild(centeredPanel);
			centeredPanel.AddChild(imperativeChild);
			centeredPanel.AddChild(scrollChild);

			root.RecalculateLayout();

			Assert.That(centeredPanel.Bounds, Is.EqualTo(new WidgetBounds(340, 160, 600, 400)));
			Assert.That(imperativeChild.Bounds, Is.EqualTo(new WidgetBounds(25, 35, 120, 48)));

			// Simulate a runtime window resolution change on macOS
			root.Bounds = new WidgetBounds(0, 0, 1920, 1080);
			root.RecalculateLayout();

			Assert.That(centeredPanel.Bounds, Is.EqualTo(new WidgetBounds(660, 340, 600, 400)));
			Assert.That(imperativeChild.Bounds, Is.EqualTo(new WidgetBounds(25, 35, 120, 48)));

			// Verify scroll panel child layout preserves Y and Height while updating Width
			centeredPanel.Bounds = new WidgetBounds(660, 340, 800, 400);
			scrollChild.Bounds = new WidgetBounds(10, 150, 580, 45);
			scrollChild.RecalculateLayout(true);

			Assert.That(scrollChild.Bounds, Is.EqualTo(new WidgetBounds(10, 150, 780, 45)));
		}
	}
}
