Act as a Principal Software Engineer. I am using GitHub Spec Kit for a Node.js Express project.
 
I want you to generate a 'constitution.md' file that strictly adheres to the "Node.js Best Practices" guide found here:
https://raw.githubusercontent.com/goldbergyoni/nodebestpractices/refs/heads/master/README.md
 
Extract and enforce the following specific rules into the Constitution:
 
1. **Project Structure**:
   - Enforce "Component-based" folder structure (keep related files like controllers, services, and tests together in a folder).
   - Separate 'API' logic from 'Domain' logic.
 
2. **Error Handling**:
   - Use only Async/Await (no callbacks).
   - Use a Centralized Error Handler (Middleware). Do not handle errors inside the routes.
   - Distinguish between "Operational Errors" and "Programmer Errors" as defined in the guide.
 
3. **Code Style & Quality**:
   - Use 'const' over 'let'.
   - Avoid 'Magic Strings'; all constants must be in a config file.
   - All functions must be small and do one thing (Single Responsibility Principle).
 
4. **Testing (The AAA Pattern)**:
   - Every feature implementation must include tests following the "Arrange, Act, Assert" pattern.
   - Focus on Integration Tests over unit tests for database logic.
 
5. **Security**:
   - Enforce the use of 'Helmet', 'CORS', and 'Rate-Limiting'.
   - Mandatory request validation using a library like 'Zod' or 'Joi'.
 
6. **Spec-Driven Governance**:
   - If I use the '/implement' command and my request violates any of these 'Node Best Practices', the AI MUST flag the violation and refuse to generate the code until the plan is corrected.