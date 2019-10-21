# FunctionSignatureComparer: A Tool for showing commits of a java-based git repository that add parameters to functions; #
    * This is a WPF/C# Tool.
    

# How To Use FunctionSignatureComparer Tool #
  
*     If you just want to run the tool


    *    You can download .exe file and run it. The Exe file is in this path : \FunctionSignatureComparer\bin\Debug\FunctionSignatureComparer.exe
    *    In the Main Window, you can see three buttons: 
        *1) Choose Git Folder: You should select your git repository folder by clicking this button and select the path.
        
        *2) Select Result File Path: by clicking this button, you can select the result file(.csv). If you don't select any file path, the results 
        *    save in a result.csv file that will be placed in git repository path that you have selected before.
        
        *3) Compare Function Signature: by clicking this button you can run the main process and get the results.
        
*    if you want to build the solution and run it 
        *    You can clone the repository and open the solution(.\FunctionSignatureComparer.sln) in Visual Studio, then build it and run it.

# Test Cases That Have Been Tested #

    * I've tested this tool with three popular Java Github repositories and lseditkconfig that is one of the academic java projects of prof. Sarah Nadi.
        *    MPAndroidChart : https://github.com/PhilJay/MPAndroidChart
        *    Retrofit : https://github.com/square/retrofit
        *    OkHttp : https://github.com/square/okhttp/
        *    lseditkconfig : https://github.com/snadi/lseditkconfig/commits/master
    * All results of these test cases are accessible in git repository of the tool in ./Results folder with their owen-Name.csv
    
    
    