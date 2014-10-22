*******************
Kulahs
*******************

Requirements:
.Net 4.0

Install:
Just copy the .exe wherever you want.

Usage:
*F1 opens this help menu.
*Once opened the window will stay on top of everything.
*You can either type in a color in HEX (including the alpha channel and the #) or you can click and hold the eyedropper and let it go over whatever you want to extract color from on your screen
*Left-click, drag the color swatch to move the window.
*Left-click the palette to cycle through palette choices.
*P to toggle a larger palette.
*Mouse-wheel (or up/down arrow keys) over the main swatch to change alpha.
*Mouse-wheel (or up/down arrow keys) + R to change the Red channel.
*Mouse-wheel (or up/down arrow keys) + G to change the Green channel.
*Mouse-wheel (or up/down arrow keys) + B to change the Blue channel.
*Mouse-wheel (or up/down arrow keys) + H to change the Hue.
*Mouse-wheel (or up/down arrow keys) + S to change the Saturation.
*Mouse-wheel (or up/down arrow keys) + V to change the Value (Brightness).
*Right-click on the color swatch to add it to the swatch collection below (it won't add duplicates.)
*Left-click on a swatch in the swatch collection to set the main swatch and text to that color.
*If you have a lot of swatches in the collection you can mouse-wheel over the collection to scroll back and forth.
*Double-click toggles the toolbar.
*Shift-click removes the current swatch from behind the swatch collection.
*Ctrl-click resets the alpha to full (FF).
*Ctrl-B will put borders around each of the colors added to the swatch collection.
*Ctrl-C will toggle the rounded corners of the main swatch.
*Ctrl-S will allow you to export and save your current swatch collection.
*Ctrl-E will exit the app (will not prompt to save your swatches!)
*You can paste properly formatted RGB values directly into the text box to convert them to HTML. Format: as long as there is an "r" then a number and the same for "g" and "b" with an optional "a", in any order, it will parse it. This  
includes "A=34, R=2, G=90, B=189" or "a34b34r235g34". As long as there is the channel and a number that is less than 256 (in that order), it will work.

Tips:
You can open multiple instances to compare color or overlap to see the impact of alpha changes.
You can see how a color with a < 100% alpha will look by putting it in the swatch collection and then setting the main swatch to something else.
If you want to see the entire swatch collection over something else or you don't want the main swatch to influence the combined color you can use the shift click to remove it.
Combined colors based on alpha differences are different colors! So if you set a color @ FF then reduce the alpha you can sample that color again as a combination of whatever is underneath.

Bugs:
Report anything you find to joel.vandenberg@bestbuy.com

Caveats:
I haven't tested this on anything but Windows 7.
