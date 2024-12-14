import { useEffect, useState } from 'react';
import { Button } from '@mui/material';

function CalenderButtons({ f, keys, handleAssign, handleOpenEvaluate }) {
    const [showAssignButton, setShowAssignButton] = useState(true);
    const [showEvaluateButton, setShowEvaluateButton] = useState(false);

    useEffect(() => {
        // const checkTime = () => {
        const currentTime = new Date();
        const scheduleDate = new Date(f.scheduleDate);
        const [endHours, endMinutes, endSeconds] = f.end.split(':').map(Number);
        scheduleDate.setHours(endHours, endMinutes, endSeconds);

        if (currentTime > scheduleDate) {
            setShowAssignButton(false);
        } else {
            setShowAssignButton(true);
        }

        const twoDaysLater = new Date(scheduleDate);
        twoDaysLater.setDate(scheduleDate.getDate() + 2);

        if (currentTime > scheduleDate && currentTime <= twoDaysLater) {
            setShowEvaluateButton(true);
        } else {
            setShowEvaluateButton(false);
        }
        // };

        // const interval = setInterval(checkTime, 1000);

        // return () => clearInterval(interval);
    }, [f.scheduleDate, f.end]);

    return (
        <>
            {showAssignButton ? (
                <Button
                    variant='contained'
                    color='primary'
                    sx={{ mt: 2, fontSize: "12px" }}
                    onClick={() => handleAssign(f, keys)}
                >
                    Gán bài tập
                </Button>
            ) : null}

            {showEvaluateButton ? (
                <Button
                    variant='contained'
                    sx={{
                        mt: 2, fontSize: "12px", bgcolor: '#16ab65',
                        '&:hover': {
                            bgcolor: '#128a51',
                        },
                    }}
                    onClick={() => handleOpenEvaluate(f, keys)}
                >
                    Đánh giá
                </Button >
            ) : null
            }
        </>
    );
}

export default CalenderButtons;
