# WoolBall

Extract type information from a .NET solution that can be used to visualize the dependencies between types in your codebase.

## Usage

    Usage:
        WoolBall <solution> <output> [options]
        
    Arguments:
        <solution>  The solution file to parse.
        <output>    The output file to write the graph to.
        
    Options:
        -e, --exclude <exclude>  Exclude types. []
        -f, --filter <filter>    Only include projects specified here. []
        --include-tests          Include test projects in the graph. [default: False]
        --orphaned               Include orphaned types in the graph. [default: False]
        -d, --display <display>  Only display the specified edges. (all, references, inheritance)) [default: all]
        --version                Show version information
        -?, -h, --help           Show help and usage information

## Visualization

The generated file can be loaded in [yEd](https://www.yworks.com/products/yed) to visualize the graph.