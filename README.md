MData
=====

What is it?
-----------

MData is a kind of DI system that allows the developper to use interfaces of the domain model, rather then the actual implementation.

This has several adavantages:
* Testing and mocking of the domain model is easy now
* AOP can be added very easily, because the concrete types are generated at runtime
* Seperation of the 'property bag' domain class, and the actual logic (methods on this domain class)

How this works
-----
There are 3 main components in this library

* Domain Class Interface (e.g. ICustomer)
* Domain Logic Class (e.g. Deriving from LogicBase<ICustomer>)
* EntityBase Base Class (Base class for runtime implementation of ICustomer)

So when an instance (or multiple instances) of ICustomer is retrieved from the DB, we actually materialize them as EntityBase<ICustomer> instances. In this runtime generated class we implement all the ICustomer members, including methods.

Thats where LogicBase<T> comes in, this enabled users to implement interface (ICustomer) defined methods. The class generator in MData will create stubs for all ICustomer's methods, and look for methods with equal definitions in the LogicBase<T> derived class. It will then forward calls made from the EntityBase derived class to the LogicBase Implementation. 

