//! This is a preserved comment


function MyLib()
{
    var x=23;
    var y=99;
    
    // private:.m_*

    function Helper()
    {
        this.m_Blah="Cool";
        
    }
    
    function Helper2()
    {
        this.m_iCounter="Cool";
        this.m_SomethingElse=outer.m_Blah;
    }
    
    // private:Helper.
    Helper.prototype.DoSomething=function()
    {
        alert("Hi there");
    }
    
    Helper.prototype.DoSomethingElse=function()
    {
        alert("Hi there");
    }
    
    var z=new Helper();
    return z.DoSomething();
    
    return x+y;
}

function MyLib2()
{
    Helper.prototype.DoSomething=function()
    {
        alert("Hi there");
    }
    
    var z=new Helper();
    return z.DoSomething();
}
