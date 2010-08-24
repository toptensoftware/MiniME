function doSomething()
{
    alert("Hello World");
    
    var scope="";
    
    var x;
    if (x==23)
    {
        alert("cool");
    }
    
    blah();
}

function doSomethingElse()
{
    switch (blah())
    {
        case 0:
        case 1:
            alert("lo");
            // fall through
            
        case 2:
        case 3:
            alert("hi");
            // fall through
            
        default:
            alert("other");
            // fall through
    }
}