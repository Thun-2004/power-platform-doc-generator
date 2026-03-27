
import * as React from 'react'
import {render, fireEvent, screen} from '@testing-library/react'
import DiagramSelectionBox from './DiagramSelectionBox.jsx'

test("Renders output selection buttons", () => {
    const type = { id: "overview", title: "Overview", desc: "A high-level summary of the solution, including key components such as canvas apps, workflows, screens, and environment variables."}

    const selectedModes = ["overview", "workflows", "faq"];
    const toggleSelected = (id) => {
        console.log(id);
    }
    const charLimit = 200;

    render(<DiagramSelectionBox key={type.id} type={type} selectedModes={selectedModes} toggleSelected={toggleSelected} charLimit={charLimit} hasExampleDoc={false}></DiagramSelectionBox>)
    expect(screen.getByText(type["desc"])).toBeInTheDocument()
})

test("Calls toggle selected when clicked", () => {
    const type = { id: "overview", title: "Overview", desc: "A high-level summary of the solution, including key components such as canvas apps, workflows, screens, and environment variables."}

    const selectedModes = ["workflows", "faq"];
    const toggleSelected = (id) => {
        render(<p>I am a test!</p>)
    }
    const charLimit = 200;


    render(<div data-testid="testid"><DiagramSelectionBox key={type.id} type={type} selectedModes={selectedModes} toggleSelected={toggleSelected} charLimit={charLimit} hasExampleDoc={false}></DiagramSelectionBox></div>)

    const button = screen.getByTestId("testid");
    fireEvent.click(button);
    expect("I am a test!").toBeInTheDocument;
})