import { Box, Button, IconButton, Modal, Typography } from '@mui/material'
import React, { useEffect, useState } from 'react'
import CloseIcon from '@mui/icons-material/Close';
import services from '~/plugins/services';
function AssessmentGuild() {
    const [open, setOpen] = React.useState(false);
    const handleOpen = () => setOpen(true);
    const handleClose = () => setOpen(false);
    const [scoreRanges, setScoreRanges] = useState([]);
    useEffect(() => {
        const getListScoreRanges = async () => {
            try {
                services.ScoreRangeAPI.getListScoreRange((res) => {
                    setScoreRanges(res.result);
                })
            } catch (error) {
                console.log(error);
            }
        }
        getListScoreRanges();
    }, [])
    return (
        <>
            <Button onClick={handleOpen}>Cách thức đánh giá?</Button>
            <Modal
                open={open}
            >
                <Box sx={{
                    position: 'absolute',
                    top: '50%',
                    left: '50%',
                    transform: 'translate(-50%, -50%)',
                    width: 800,
                    bgcolor: 'background.paper',
                    boxShadow: 24,
                    p: 4,
                }}>
                    <IconButton onClick={handleClose} sx={{ position: "absolute", top: 5, right: 5 }}><CloseIcon /></IconButton>
                    <Typography variant='h5'>Hướng dẫn thực hiện bài Test, chấm điểm và đánh giá</Typography>
                    <Typography>Thang đánh giá gồm 15 vấn đề, mỗi mục có 4 mức độ. Người đánh giá quan sát trẻ, đánh giá các hành vi tương ứng với mỗi mức độ của mục đó.</Typography>
                    <ul>
                        <li>Bình thường: 4 điểm</li>
                        <li>Bất thường nhẹ: 3 điểm</li>
                        <li>Bất thường trung bình: 2 điểm</li>
                        <li>Bất thường nặng: 1 điểm</li>
                        <li>Lưu ý: Bạn có thể dùng các mức thang đánh giá 1.5, 2.5 hoặc 3.5 nếu đứa trẻ đó ở mức tương đối giữa các tiêu chí trên.</li>
                    </ul>
                    <Typography variant='h5'>Kết quả đánh giá tự kỷ </Typography>
                    <Typography sx={{ color: "red", fontSize: "12px" }}>(Lưu lý số điểm này chỉ đúng với số lượng đánh giá hiện tại)</Typography>
                    <Typography>Tổng điểm được tính bằng cách cộng số điểm mỗi câu:</Typography>
                    <ul>
                        {
                            scoreRanges && scoreRanges.lenght !== 0 && scoreRanges.map((s) => {
                                return (
                                    <li key={s.id}>Nếu tổng điểm từ {s.minScore} - {s.maxScore} điểm: {s.description}</li>
                                )
                            })
                        }
                    </ul>
                </Box>
            </Modal>
        </>
    )
}

export default AssessmentGuild
