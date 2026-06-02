

FragmentProperties LoadFragmentPropertiesCanColor(vec3 canColor)
{
    FragmentProperties frag = LoadFragmentProperties();

    const vec3 targetBaseColor = vec3(0.75391, 0.48438, 0.01367);
    const float tolerance = 0.4;

    if (distance(frag.albedo, targetBaseColor) <= tolerance)
    {
        frag.albedo = canColor;
    }

    return frag;
}