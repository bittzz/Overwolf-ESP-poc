var plugin = new OverwolfPlugin("esp-poc-plugin", true);

plugin.initialize(function(status) 
{
	if (status == false) 
	{
		console.log("Plugin couldn't be loaded??");
		return;
	}
});

var inFocus = false;
async function doDrawing() 
{
	var mySVG = document.getElementById("mySVG");
	var parentSVG = mySVG.parentNode;
		
	while(inFocus)
	{
		var newSVG = mySVG.cloneNode(false);
		
		var BoxArray = plugin.get().BoxArray;
		for (i = 0; i < BoxArray.length; i++) 
		{ 		
			newSVG.appendChild(createRect(BoxArray[i]));
		}
				
		parentSVG.replaceChild(newSVG, mySVG);
		mySVG = newSVG;

		gameLoop();
		document.getElementById("FPS").innerHTML = (1000/frameTime).toFixed(1) + " fps";

		await sleep(1);
	}
	plugin.run().StopThread();
}


function createRect(playerData)
{
	var myRect = document.createElementNS("http://www.w3.org/2000/svg","rect");
	myRect.setAttributeNS(null,"fill","none");
	myRect.setAttributeNS(null,"stroke","red");
	myRect.setAttributeNS(null,"x",playerData[0]);
	myRect.setAttributeNS(null,"y",playerData[1]);
	myRect.setAttributeNS(null,"height", playerData[2]);
	myRect.setAttributeNS(null,"width", playerData[3]);
	
	return myRect;
}
	
overwolf.games.onGameInfoUpdated.addListener(function (GameInfoChangeData) 
{
	if (GameInfoChangeData.gameInfo.isRunning && GameInfoChangeData.gameInfo.isInFocus)
	{
		overwolf.windows.getCurrentWindow(function(result)
		{
			overwolf.windows.maximize(result.window.id);
		});
		

		if (!plugin.get().IsInitialized)
		{
			plugin.run().Initialize(GameInfoChangeData.gameInfo.width, GameInfoChangeData.gameInfo.height);
		}
		
		inFocus = GameInfoChangeData.gameInfo.isInFocus;
		if (inFocus)
		{
			if (!plugin.get().IsRunning)
			{
				plugin.run().StartThread();
			}
		}
		else
		{
			plugin.run().StopThread();
		}
		sleep(5000);
		
		doDrawing();
	}
});

overwolf.games.getRunningGameInfo(function (gameInfo) 
{
	if (gameInfo.isRunning && gameInfo.isInFocus)
	{
		overwolf.windows.getCurrentWindow(function(result)
		{
			overwolf.windows.maximize(result.window.id);
		});
		

		if (!plugin.get().IsInitialized)
		{
			plugin.run().Initialize(gameInfo.width, gameInfo.height);
		}
		
		inFocus = gameInfo.isInFocus;
		if (inFocus)
		{
			if (!plugin.get().IsRunning)
			{
				plugin.run().StartThread();
			}
		}
		else
		{
			plugin.run().StopThread();
		}
		sleep(5000);
		
		doDrawing();
	}
});

// Sleep function			
function sleep(ms) 
{
	return new Promise(resolve => setTimeout(resolve, ms));
}

// FPS function
var filterStrength = 20;
var frameTime = 0, lastLoop = new Date, thisLoop;

function gameLoop(){
  // ...
  var thisFrameTime = (thisLoop=new Date) - lastLoop;
  frameTime+= (thisFrameTime - frameTime) / filterStrength;
  lastLoop = thisLoop;
}

