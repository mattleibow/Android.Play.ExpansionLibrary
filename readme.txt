This is the C# implementation of Google's License Verification Library and the Android Play Expansion Library

It is a bit scary as this is a direct translation f the code and I am still busy .NETifying it. 

They compile and the LVL works. I am busy cleaning this up. I haven't been able to test the Expansions bit yet as I don't hav a version 4.0 device yet. I will try and get the version down to 2.1 or so. Currently due to Mono for Android's limitation on including resource files with class libraries, it won't compile if you include the v3 notifications bit. Removing it will allow itto compile, but I haven't got around to testing this on the v11+ devices yet.

Maybe someone could do this to help us?

This is most of the work done and it just needs cleaning and refactoring.