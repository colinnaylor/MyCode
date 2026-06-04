<script>
	const cloud = document.getElementById('Cloud');

	cloud.addEventListener('mouseover', () => {
		cloud.innerHTML = '<h3>Cloud Solutions</h3>\n<p>Data storage in the cloud in a SQL database or similar, accessible by webpages and your onsite solutions.</p>';
	});

	cloud.addEventListener('mouseout', () => {
		cloud.innerHTML = '<h3>Cloud Solutions</h3>\n<p>Secure and scalable cloud computing services.</p>';
	});

	const cyber = document.getElementById('Cyber');

	cyber.addEventListener('mouseover', () => {
		cyber.innerHTML = '<h3>Cybersecurity</h3>\n<p>Ensure that only the people that you want can access your data.</p>';
	});

	cyber.addEventListener('mouseout', () => {
		cyber.innerHTML = '<h3>Cybersecurity</h3><p>Protect your business with cutting-edge security solutions.</p>';
	});

	const consult = document.getElementById('Consultancy');

	consult.addEventListener('mouseover', () => {
		consult.innerHTML = '<h3>IT Consultancy</h3>\n<p>Advice on client-server, webpages, desktop applications, databases, workflows, services and good user experiences.</p>';
	});

	consult.addEventListener('mouseout', () => {
		consult.innerHTML = '<h3>IT Consultancy</h3><p>Expert advice to drive your business forward.</p>';
	});

</script>
