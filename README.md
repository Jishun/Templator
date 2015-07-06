# Templator
An advanced text replacing template engine designed for using simplified dictionary as input
it's a good helper for handling data processing targeting to multiple document formats

	Support plain text and xml
	Input requirement extraction from template 
	Configurable field definition
	Build-in validation based on template
	Array/Repeat support
	Constant input and calculating support
	
Examples:

with a template like this 
	{{Field1}}Any Free Text Which doesn't conflict with the text holder reserved marks{{User(Repeat)[Collection]}}{{Field2}} and {{User(Repeat)[CollectionEnd]}}...
it will generate a required input list as [{Name: "Repeat" , Children: [{Name: "Field2"}]}, {Name: "Field1"}]
given input as : 
	{Repeat: [{Field2:","},{Field2:"only replaces what's defined"}], Field1:"This will work with "}
the result string would be:
	This will work with Any Free Text Which doesn't conflict with the text holder reserved marks, and only replaces what's defined and ...

plain text is even simpler
	the engine will replace on-the-go to replace the text holders with what's in the input dictionary with doing the necessary validation against the input
	
detailed documents on the way..

