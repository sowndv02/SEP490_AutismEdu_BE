import { Box } from '@mui/material'
import React from 'react'

function CenterLocation() {
    return (
        <Box sx={{ width: "65%" }}>
            <iframe src="https://www.google.com/maps/embed?pb=!1m18!1m12!1m3!1d3724.5062169040166!2d105.52271427471399!3d21.012421688340613!2m3!1f0!2f0!3f0!3m2!1i1024!2i768!4f13.1!3m3!1m2!1s0x3135abc60e7d3f19%3A0x2be9d7d0b5abcbf4!2zVHLGsOG7nW5nIMSQ4bqhaSBo4buNYyBGUFQgSMOgIE7hu5lp!5e0!3m2!1svi!2s!4v1726547831131!5m2!1svi!2s"
                style={{
                    width: "100%",
                    height: "450px",
                    border: "none",
                    borderRadius:"5px"
                }}
                allowfullscreen="" loading="lazy" referrerPolicy="no-referrer-when-downgrade"></iframe>
        </Box>
    )
}

export default CenterLocation
