// components/SafePreviewBoundary.jsx
import React from 'react';

class SafePreviewBoundary extends React.Component {
  constructor(props) {
    super(props);
    this.state = { hasError: false, message: null };
  }

  static getDerivedStateFromError(error) {
    return { hasError: true, message: error?.message || 'Preview failed.' };
  }

  componentDidCatch(error, info) {
    console.error('Preview error', error, info);
  }

  render() {
    if (this.state.hasError) {
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