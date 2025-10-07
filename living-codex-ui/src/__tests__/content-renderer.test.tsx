import React from 'react'
import { screen, render } from '@testing-library/react'
import { ContentRenderer } from '@/components/renderers/ContentRenderer'
jest.unmock('@/components/renderers/ContentRenderer')

describe('ContentRenderer', () => {
  it('should preserve newlines in text/plain content', () => {
    const textWithNewlines = 'Line 1\nLine 2\n\nLine 3'
    
    render(
      <ContentRenderer 
        content={{ 
          mediaType: 'text/plain', 
          inlineJson: textWithNewlines 
        }} 
      />
    )
    
    // Check that the content is rendered with preserved newlines
    const preElement = document.querySelector('pre')
    expect(preElement).toBeInTheDocument()
    expect(preElement?.tagName).toBe('PRE')
    expect(preElement).toHaveClass('whitespace-pre-wrap')
    expect(preElement?.textContent).toBe(textWithNewlines)
  })

  it('should handle empty text/plain content', () => {
    render(
      <ContentRenderer 
        content={{ 
          mediaType: 'text/plain', 
          inlineJson: '' 
        }} 
      />
    )
    
    // Should fallback to DefaultRenderer for empty content
    expect(screen.getByText('No content available')).toBeInTheDocument()
  })

  it('should handle multiline text with proper formatting', () => {
    const multilineText = `First line
Second line

Paragraph break
Another line`
    
    render(
      <ContentRenderer 
        content={{ 
          mediaType: 'text/plain', 
          inlineJson: multilineText 
        }} 
      />
    )
    
    const preElement = document.querySelector('pre')
    expect(preElement).toBeInTheDocument()
    expect(preElement).toHaveClass('whitespace-pre-wrap', 'break-words')
    expect(preElement?.textContent).toBe(multilineText)
  })

  it('should render text/plain content in a styled container', () => {
    const textContent = 'Simple text content'
    
    render(
      <ContentRenderer 
        content={{ 
          mediaType: 'text/plain', 
          inlineJson: textContent 
        }} 
      />
    )
    
    // Check the container styling
    const container = screen.getByText(textContent).closest('div')
    expect(container).toHaveClass('bg-white', 'dark:bg-gray-800', 'rounded-lg', 'border')
    
    // Check the pre element styling
    const preElement = screen.getByText(textContent)
    expect(preElement).toHaveClass('whitespace-pre-wrap', 'break-words', 'text-sm')
  })
})
