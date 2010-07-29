function DoSomething()
{
    var x=23;
    var y=99;
    
    y++;
    
    function inner(x)
    {
        return x++;
    }
}
