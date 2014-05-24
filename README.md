SharpAngie
==========

data binding between c# view model and angular.js view

##Motivation
- We needed an easy way to develop GUIs for a 3D application in XNA.
- Existing GUI frameworks for XNA were too uncomfortable because they are usually written in C# like WinForms.
- HTML is a standardized way to describe user interfaces and with angular.js a good data binding framework is available.
- angular.js binds to JavaScript objects, but we like to write UI logic in C# view models for better interaction with the application logic.

##What you need
- [Awesomium for .NET](http://www.awesomium.com/) if you want to run the demo application.

##What you get
- C# view models with minimal code. [Example](/BaamStudios.SharpAngieDemo/DemoViewModel.cs)
- HTML views with vanilla angular.js syntax. No custom "ng-" attributes necessary. [Example](/BaamStudios.SharpAngieDemo/DemoView.html)
- TwoWay data binding.

##Instructions
- Implement your view model like [this](/BaamStudios.SharpAngieDemo/DemoViewModel.cs).
- Implement your HTML view like [this](/BaamStudios.SharpAngieDemo/DemoView.html).
- Implement [IAngularInterface](/BaamStudios.SharpAngie/IAngularInterface.cs) with your custom code to forward the calls from C# to JavaScript. See [WebViewBridge](/BaamStudios.SharpAngieDemo/WebViewBridge.cs) for an awesomium implementation.
- Create an instance of [Bridge](/BaamStudios.SharpAngie/Bridge.cs).
- Create the JavaScript object [window.sharpAngieBridge](/BaamStudios.SharpAngie/Bridge.cs) with your custom code to forward the calls from JavaScript to C#. See [WebViewBridge](/BaamStudios.SharpAngieDemo/WebViewBridge.cs) for an awesomium implementation.

##Limitations
- The HTML view can only directly change simple data type fields. It cannot create or move objects in the view model tree. However, complex view logic can be implemented as a method in the view model and executed by the view.
- A nested view model must only be referenced once in the view model tree because each nested view model object keeps a reference to the parent object. This parent/child relationship is used to traverse the view model tree via property paths like "propertyX.propertyY[42].propertyZ".
