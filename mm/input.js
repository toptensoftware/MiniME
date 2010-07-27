if (p.current() == ' ')
{
	if (tabPos < 0)
		leadingSpaces++;
}
else if (p.current() == '\t')
{
	if (tabPos < 0)
		tabPos = p.position;
}
else
{
	// Something else, get out
	break;
}
