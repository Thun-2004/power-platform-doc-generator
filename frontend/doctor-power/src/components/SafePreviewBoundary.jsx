// components/SafePreviewBoundary.jsx
import React from 'react';

class SafePreviewBoundary extends React.Component {
  constructor(props) {
    super(props);
    this.state = { hasError: false, message: null };
  }

  static getDerivedStateFromError(error) { //when child component throws an error
    return { hasError: true, message: error?.message || 'Preview failed.' };
  }

  componentDidCatch(error, info) { //when caught an error
    console.error('Preview error', error, info);
  }

  render() {
    if (this.state.hasError) { //if there is an error, show the error message
      return (
        <div className="p-4 bg-red-50 text-red-700 rounded-lg">
          {this.state.message}
        </div>
      );
    }
    return this.props.children;
  }
}

export default SafePreviewBoundary;