var isLoggedIn = false;

function Login(){
	console.log("logging in");

	const params = {
		usernameCode: 'c1ce28880ddb779ddd3a1b8725e80a2ff5cf32fcecf38da128beb092f0058a01',
		accessCode: 'dd5777c4632eb5de69f6f46f309ac2d9f3c0489e12c3ebd41d9855b1e27badce'
	};
	
	const options = {
    method: 'POST',
    body: JSON.stringify( params )  
	};
	const apiUrl = 'https://rubricsapiapp.azurewebsites.net/Api/Document/PostLogin';	
	//const apiUrl = 'https://localhost:44301/api/Document/GetLogin';	
	
	console.log("calling fetch method");
	
	fetch(apiUrl, options)
	    .then( response => {
		console.log("A response");
		if (!response.ok) {
		  if (response.status === 404) {
			throw new Error('Data not found - 404');
		  } else if (response.status === 500) {
			throw new Error('Server error');
		  } else {
			throw new Error('Network response was not ok');
		  }
		}
		return response.json();
		})
		.then( response => {
        console.log("We have a return");
        console.log(response);
		} 
	);
	
	document.getElementById('loginPage').style.display='none';
	document.getElementById('mainPage').style.display='block';
	document.getElementById('fishPage').style.display = 'none';
	document.getElementById('foodPage').style.display = 'none';
	document.getElementById('examplesPage').style.display = 'none';
	
}
function Logout(){
	console.log("logging out");
	document.getElementById('loginPage').style.display='block';
	document.getElementById('mainPage').style.display='none';
	document.getElementById('fishPage').style.display = 'none';
	document.getElementById('foodPage').style.display = 'none';
	document.getElementById('examplesPage').style.display = 'none';
}
function ShowFishPage() {
	document.getElementById('loginPage').style.display = 'none';
	document.getElementById('mainPage').style.display = 'none';
	document.getElementById('fishPage').style.display = 'block';
	document.getElementById('foodPage').style.display = 'none';
	document.getElementById('examplesPage').style.display = 'none';
}

function ShowFood() {
	document.getElementById('loginPage').style.display = 'none';
	document.getElementById('mainPage').style.display = 'none';
	document.getElementById('fishPage').style.display = 'none';
	document.getElementById('foodPage').style.display = 'block';
	document.getElementById('examplesPage').style.display = 'none';
}

function ShowExamples() {
	document.getElementById('loginPage').style.display = 'none';
	document.getElementById('mainPage').style.display = 'none';
	document.getElementById('fishPage').style.display = 'none';
	document.getElementById('foodPage').style.display = 'none';
	document.getElementById('examplesPage').style.display = 'block';
}

