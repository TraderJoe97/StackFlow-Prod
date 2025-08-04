// aiAssistant.js

let userJwt = null;

// 1. Initialize the AI assistant
async function initializeAIAssistant() {
    console.log("Initializing AI Assistant...");
    try {
        userJwt = await getJwtForUser();
        if (userJwt) {
            console.log("AI Assistant initialized successfully.");
            // Potentially show the AI assistant UI or enable its features here
        } else {
            console.error("Failed to initialize AI Assistant: Could not obtain JWT.");
            // Handle the case where JWT is not available (e.g., user not logged in)
        }
    } catch (error) {
        console.error("Error during AI Assistant initialization:", error);
    }
}

// 2. Make a request to the new JWT endpoint
async function getJwtForUser() {
    try {
        const response = await fetch('/api/Account/GetJwt', { // Assuming the new endpoint is /api/Account/GetJwt
            method: 'GET',
            headers: {
                'Content-Type': 'application/json'
            }
            // Cookie will be automatically sent by the browser for the same origin
        });

        if (response.ok) {
            const data = await response.json();
            // 3. Store and manage the retrieved JWT securely
            // In a real application, consider more secure storage than a variable
            return data.token; // Assuming the JWT is returned in a 'token' field
        } else {
            console.error("Failed to get JWT:", response.status, response.statusText);
            return null;
        }
    } catch (error) {
        console.error("Error fetching JWT:", error);
        return null;
    }
}

// 4. Implement a function for AI to call other APIs
async function callApiWithJwt(endpoint, method = 'GET', data = null) {
    if (!userJwt) {
        console.error("Cannot call API: JWT is not available.");
        return null;
    }

    const headers = {
        'Content-Type': 'application/json',
        'Authorization': `Bearer ${userJwt}`
    };

    const requestOptions = {
        method: method,
        headers: headers,
    };

    if (data && (method === 'POST' || method === 'PUT' || method === 'PATCH')) {
        requestOptions.body = JSON.stringify(data);
    }

    try {
        const response = await fetch(endpoint, requestOptions);

        if (response.ok) {
            // Check if the response has content before parsing as JSON
            const text = await response.text();
            return text ? JSON.parse(text) : {};
        } else {
            console.error(`API call failed to ${endpoint}:`, response.status, response.statusText);
            return null; // Or throw an error
        }
    } catch (error) {
        console.error(`Error calling API ${endpoint}:`, error);
        return null; // Or throw an error
    }
}

// 5. Define the orchestrator logic for the AI assistant
async function aiOrchestrator(userInput) {
    if (!userJwt) {
        return "AI Assistant is not initialized. Please log in.";
    }

    // Simple example orchestrator: Analyze input and decide which tool to use
    // In a real application, this would involve more sophisticated NLP and reasoning
    const lowerInput = userInput.toLowerCase();
    let toolResult = null;

    if (lowerInput.includes("list projects")) {
        toolResult = await listProjectsTool();
    } else if (lowerInput.includes("list my tickets")) {
        toolResult = await listMyTicketsTool();
    }
    // Add more conditions for other tools

    if (toolResult !== null) {
        return formatToolResult(toolResult); // Format the tool's output for the user
    } else {
        return "I'm sorry, I can't help with that request yet.";
    }
}

// 6. Define the tool definitions
// Leverage existing API DTOs for data structure where applicable

async function listProjectsTool() {
    const endpoint = '/api/ProjectApi/ListProjects'; // Example endpoint
    // Assuming ProjectDto exists and matches the response structure
    // This tool calls the API to get a list of projects
    return await callApiWithJwt(endpoint, 'GET');
}

async function listMyTicketsTool() {
    const endpoint = '/api/TicketsApi/ListTickets'; // Example endpoint
    // Assuming this endpoint supports filtering for the current user's tickets
    // Or you might need a specific endpoint like '/api/TicketsApi/ListMyTickets'
    // Assuming TicketDto exists and matches the response structure
     return await callApiWithJwt(endpoint, 'GET');
}

// Example tool for creating a ticket - requires input data
async function createTicketTool(ticketData) {
     const endpoint = '/api/TicketsApi/CreateTicket'; // Example endpoint
     // Assuming CreateTicketDto exists and matches the request body structure
     // Assuming TicketDto exists and matches the response structure
     return await callApiWithJwt(endpoint, 'POST', ticketData);
}


// Add more tool functions for other allowed APIs
// For example:
// async function getProjectDetailsTool(projectId) { ... }
// async function addCommentToTicketTool(ticketId, commentData) { ... }
// etc.

// Helper function to format tool results for the user
function formatToolResult(result) {
    // Basic formatting based on expected DTO structures
    if (Array.isArray(result)) {
        if (result.length > 0 && result[0].projectName) { // Assuming projectName for ProjectDto
            return "Here are the projects:\n" + result.map(p => `- ${p.projectName}`).join('\n');
        } else if (result.length > 0 && result[0].title) { // Assuming title for TicketDto
             return "Here are your tickets:\n" + result.map(t => `- ${t.title} (Status: ${t.status})`).join('\n');
        }
    } else if (result && result.id) { // Assuming an object with an ID indicates a created item
         return `Successfully created ${result.title || 'item'}. ID: ${result.id}`;
    }

    return JSON.stringify(result, null, 2); // Default to raw JSON if structure is unknown
}


// Example usage:
// Call initializeAIAssistant() when the user is determined to be logged in on the page.
// For example, in a script tag within the authenticated view:
// <script>
//   document.addEventListener('DOMContentLoaded', function() {
//     initializeAIAssistant();
//   });
// </script>

// Then, when a user interacts with the AI (e.g., types in a chat box):
// async function handleUserAIChat(message) {
//   const response = await aiOrchestrator(message);
//   displayAIResponse(response); // Function to display the response on the page
// }