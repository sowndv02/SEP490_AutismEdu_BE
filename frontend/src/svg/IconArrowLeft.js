import React from 'react';
import PropTypes from 'prop-types';

const ArrowLeftIcon = ({ iconName, width, height, fill }) => (
  <svg 
    width={width} 
    height={height} 
    viewBox="0 0 18 18" 
    fill="none" 
    xmlns="http://www.w3.org/2000/svg"
  >
    <path 
      d="M9.62042 13.0463L4.08625 7.5L9.62042 1.95375L7.91667 0.25L0.666672 7.5L7.91667 14.75L9.62042 13.0463Z" 
      fill={fill} 
    />
  </svg>
);

ArrowLeftIcon.propTypes = {
  iconName: PropTypes.string,
  width: PropTypes.oneOfType([PropTypes.number, PropTypes.string]),
  height: PropTypes.oneOfType([PropTypes.number, PropTypes.string]),
  fill: PropTypes.string,
};

ArrowLeftIcon.defaultProps = {
  iconName: 'icon-arrow-left',
  width: 18,
  height: 18,
  fill: "#ddd",
};

export default ArrowLeftIcon;
