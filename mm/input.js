var a;

function b()
{
}

function test(x, y, z)
{
    var i;
    var j;
    var k;
    
    k=k+k+k+k+x;
    this.blah++;
    
    function child1(x)
    {
        i=(j+k)/x;
        alert("Hello World");
        this.variable=this.variable+this.offset*3;
        nested_again(x);
        
        if (i==0)
        {
            alert(x);
            alert(y);
        }
        else
        {
            alert(i);
            alert(i);
        }
        
        for (var i=0; i<100; i++)
        {
            alert(i);
            alert(i);
        }
        
        for (var i=0; i<100; i++)
        {
            alert(i);
        }
        
        while (true)
        {
            DoSomething();
            DoSomething();
        }
        while (true)
        {
            DoSomething();
        }
        
        function nested_again(z)
        {
            return z+k+k+k+k+k+k+k+k+k+k+k+k;
        }
    }
    function child2(p,q)
    {
        return p+q;
    }
    
    var something_else=23;
    
    child1(i);
    child2(j,k);
}

var c;
