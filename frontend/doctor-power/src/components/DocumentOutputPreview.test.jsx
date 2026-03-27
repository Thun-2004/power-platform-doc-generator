import * as React from 'react'
import {render, fireEvent, screen} from '@testing-library/react'
import DocumentOutputPreview from './DocumentOutputPreview.jsx'

test("null if no output", () => {
    render(<DocumentOutputPreview outputItems={[]} setPreviewFile={() => {}} onDismiss={() => {}} onRegenerate={() => {}}></DocumentOutputPreview>);
    expect(screen).toBeEmptyDOMElement
})

test("Render multiple inputs", () => {
    render(<DocumentOutputPreview outputItems={["overview", "faq"]} setPreviewFile={() => {}} onDismiss={() => {}} onRegenerate={() => {}}></DocumentOutputPreview>);
    expect(screen).not.toBeNull;    
})