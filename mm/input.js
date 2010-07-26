function test()
{
    function sub()
    {
        with(blah)
        {
            DoSomething();
        }
    }
    
    function sub2()
    {
        return eval("sub()");
    }
    
    function sub3(param1, param2)
    {
        return param1 + param2;
    }
    
    sub();
}
