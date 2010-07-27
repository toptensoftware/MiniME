
function test(ch)
{
    switch (ch)
    {
        case 1:
        case 2:
        case 3:
            break;
        
        case 5:
            return "blah";
            
          
    }
    
    return "deblah";
}

alert(test(1));