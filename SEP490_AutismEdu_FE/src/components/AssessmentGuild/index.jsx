import { Box, Button, IconButton, Modal, Typography } from '@mui/material'
import React from 'react'
import CloseIcon from '@mui/icons-material/Close';
function AssessmentGuild() {
    const [open, setOpen] = React.useState(false);
    const handleOpen = () => setOpen(true);
    const handleClose = () => setOpen(false);
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
                    <Typography>Tổng điểm được tính bằng cách cộng số điểm mỗi câu:</Typography>
                    <ul>
                        <li>Nếu tổng điểm từ 36 - 60 điểm: Trẻ bình thường</li>
                        <li>Nếu tổng điểm từ 30 - 36 điểm: Tự kỷ nhẹ đến trung bình</li>
                        <li>Nếu tổng điểm từ 15 - 30 điểm: Tự kỷ nặng.</li>
                    </ul>
                </Box>
            </Modal>
        </>
    )
}

export default AssessmentGuild
