using System;
using Future.UX.SourceGenerators.TEST;
using Future.UX.MVVM;

Person p = new();
p.PropertyChanged += (s, e) =>
{
    Console.WriteLine($"PropertyChanged: {e.PropertyName}");
};

// Set basic fields
p.FirstName = "John";
p.LastName = "Doe";
p.Title = "Mr.";
p.Email = "john.doe@example.com";
p.Phone = "+44 1234 567890";
p.Address = "221B Baker Street, London";

if(p.PlayCommand.CanExecute("HI")) (p.PlayCommand)?.Execute("HI");

// Access Fullname (computed property)
Console.WriteLine($"Fullname: {p.Fullname}");

// Another computed property that depends on Fullname
Console.WriteLine($"Greeting: {p.Greeting}");

Console.ReadLine();