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
	<x><zz Bindings="{{User(Repeat)[Collection()]}}" att="{{Level1ItemName}}"><c Bindings="{{(Repeat2)[Collection,If]}}"></c></zz></x>	
it will generate a required input list as [{Name: "Repeat" , Children: [{Name: "Level1ItemName", Children: [{Name: "Repeat2", Optional: true}]}] }}]
given input as : 
	{Repeat: [{Level1ItemName:"Level1", Level1ItemName2:"Level12", Repeat2:[{},{}]},{Level1ItemName:"Level21", Level1ItemName2:"Level122"}]}
the result xml would be 
	<x> <zz att="Level12"><c></c><c></c></zz> <zz att="Level122"/> </x>

plain text is even simpler
	the engine will replace on-the-go to replace the text holders with what's in the input dictionary with doing the necessary validation against the input
	
detailed documents on the way..

